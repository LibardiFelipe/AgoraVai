using AgoraVai.WebAPI.Channels;
using AgoraVai.WebAPI.Entities;
using AgoraVai.WebAPI.Repositories;
using System.Threading.Channels;

namespace AgoraVai.WebAPI.Jobs
{
    public class PaymentPersistingJob : BackgroundService
    {
        private readonly ChannelReader<Payment> _reader;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentPersistingJob> _logger;

        public PaymentPersistingJob(
            PersistenceChannel channel,
            IServiceProvider serviceProvider,
            ILogger<PaymentPersistingJob> logger)
        {
            _reader = channel.GetReader();
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var paymentRepository = scope.ServiceProvider
                .GetRequiredService<IPaymentRepository>();

            await foreach (var payment in _reader.ReadAllAsync(stoppingToken)
                .WithCancellation(stoppingToken))
            {
                try
                {
                    await paymentRepository.InsertAsync(payment)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex, "Falha ao persistir o pagamento de CorrelationId {Id}.", payment.CorrelationId);
                }
            }
        }
    }
}
