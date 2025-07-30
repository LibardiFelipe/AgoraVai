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
        private readonly BlockingCollection<string> _messageQueue = new(10000);

        public PaymentMetricsJob(
            BrokerConfig brokerConfig,
            PaymentRepository paymentRepository)
        {
            _brokerConfig = brokerConfig;
            _paymentRepository = paymentRepository;

            _subscriber = new SubscriberSocket();
            _subscriber.Bind(brokerConfig.GetConnString());
            _subscriber.Subscribe(brokerConfig.Topic);
            _subscriber.Options.ReceiveHighWatermark = 10000;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var receiveTask = Task.Run(() => ReceivePayments(stoppingToken), stoppingToken);
            var processTask = Task.Run(() => ProcessPaymentsInBatch(stoppingToken), stoppingToken);

            await Task.WhenAll(receiveTask, processTask);
        }

        private void ReceivePayments(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_subscriber.TryReceiveFrameString(TimeSpan.FromMilliseconds(100), out var message))
                {
                    try
                    {
                        _messageQueue.Add(message, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _messageQueue.CompleteAdding();
        }

        private async Task ProcessPaymentsInBatch(CancellationToken stoppingToken)
        {
            var batch = new List<string>(capacity: 100);
            var maxBatchSize = 100;
            var maxDelay = TimeSpan.FromMilliseconds(40);
            var lastFlushTime = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                string message;
                var delayExceeded = (DateTime.UtcNow - lastFlushTime) >= maxDelay;

                if (_messageQueue.TryTake(out message!, 5, stoppingToken))
                    batch.Add(message);

                if (batch.Count >= maxBatchSize || (batch.Count > 0 && delayExceeded))
                {
                    await ProcessBatchAsync(batch);
                    batch.Clear();
                    lastFlushTime = DateTime.UtcNow;
                }
            }

            if (batch.Count > 0)
                await ProcessBatchAsync(batch);
        }

        private Task ProcessBatchAsync(List<string> messages)
        {
            foreach (var raw in messages)
            {
                var message = raw.Replace($"{_brokerConfig.Topic} ", "");
                var parts = message.Split('_');

                if (parts.Length != 3)
                    continue;

                try
                {
                    var processor = parts[0];
                    var amount = Convert.ToDecimal(parts[1]);
                    var date = DateTime.Parse(parts[2]);

                    _paymentRepository.Add(new Payment
                    {
                        Processor = processor,
                        Amount = amount,
                        Date = date
                    });
                }
                catch
                {
                    /* Ignora */
                }
            }

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _subscriber?.Dispose();
            base.Dispose();
        }
    }
}