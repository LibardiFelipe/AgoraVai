using AgoraVai.Shared.Configs;
using AgoraVai.WebAPI.Channels;
using AgoraVai.WebAPI.Entities;
using AgoraVai.WebAPI.Publishers;
using System.Threading.Channels;

namespace AgoraVai.WebAPI.Jobs
{
    public sealed class PaymentPersistingJob : BackgroundService
    {
        private readonly BrokerConfig _brokerConfig;
        private readonly Publisher _publisher;
        private readonly ChannelReader<Payment> _reader;

        public PaymentPersistingJob(
            BrokerConfig brokerConfig,
            Publisher publisher,
            PersistenceChannel channel)
        {
            _brokerConfig = brokerConfig;
            _publisher = publisher;
            _reader = channel.GetReader();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await foreach (var teste in _reader.ReadAllAsync(stoppingToken)
                    .ConfigureAwait(false)
                    .WithCancellation(stoppingToken))
                        _publisher.PublishMessage(
                            $"{_brokerConfig.Topic} {teste.ProcessedBy}_{teste.Amount}_{teste.RequestedAt}");
            
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
