using AgoraVai.Metrics.WebAPI.Entities;
using AgoraVai.Metrics.WebAPI.Repositories;
using AgoraVai.Shared.Configs;
using NetMQ;
using NetMQ.Sockets;
using System.Collections.Concurrent;

namespace AgoraVai.Metrics.WebAPI.Jobs
{
    public sealed class PaymentMetricsJob : BackgroundService
    {
        private readonly BrokerConfig _brokerConfig;
        private readonly PaymentRepository _paymentRepository;
        private readonly SubscriberSocket _subscriber;
        private readonly ConcurrentQueue<string> _messageQueue = [];

        public PaymentMetricsJob(
            BrokerConfig brokerConfig,
            PaymentRepository paymentRepository)
        {
            _brokerConfig = brokerConfig;
            _paymentRepository = paymentRepository;
            
            _subscriber = new SubscriberSocket();
            _subscriber.Bind(brokerConfig.GetConnString());
            _subscriber.Subscribe(brokerConfig.Topic);
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var receiveTask = Task.Run(() => ReceivePayments(stoppingToken), stoppingToken);
            var processTask = Task.Run(() => ProcessPayment(stoppingToken), stoppingToken);

            await Task.WhenAll(receiveTask, processTask);
        }

        private void ReceivePayments(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_subscriber.TryReceiveFrameString(
                    TimeSpan.FromMilliseconds(100), out var message))
                    _messageQueue.Enqueue(message);
            }
        }

        private void ProcessPayment(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (_messageQueue.TryDequeue(out var message))
                {
                    message = message.Replace($"{_brokerConfig.Topic} ", "");
                    var splits = message.Split('_');
                    var processor = splits[0];
                    var amount = Convert.ToDecimal(splits[1]);
                    var date = DateTime.Parse(splits[2]);

                    _paymentRepository.Add(new Payment
                    {
                        Amount = amount,
                        Date = date,
                        Processor = processor
                    });
                }
            }
        }

        public override void Dispose()
        {
            _subscriber?.Dispose();
            base.Dispose();
        }
    }
}
