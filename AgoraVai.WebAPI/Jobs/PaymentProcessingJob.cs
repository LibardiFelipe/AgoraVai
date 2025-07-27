using AgoraVai.WebAPI.Channels;
using AgoraVai.WebAPI.Entities;
using AgoraVai.WebAPI.Requests;
using AgoraVai.WebAPI.Services;
using AgoraVai.WebAPI.Utils;
using System.Diagnostics;
using System.Threading.Channels;

namespace AgoraVai.WebAPI.Jobs
{
    public class PaymentProcessingJob : BackgroundService
    {
        private readonly ChannelReader<NewPaymentRequest> _processingReader;
        private readonly PersistenceChannel _persistenceReader;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentProcessingJob> _logger;

        public PaymentProcessingJob(
            ProcessorChannel processorChannel,
            PersistenceChannel persistenceChannel,
            IServiceProvider serviceProvider,
            ILogger<PaymentProcessingJob> logger)
        {
            _processingReader = processorChannel.GetReader();
            _persistenceReader = persistenceChannel;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int batchSize = 100;
            const int maxParallelism = 10;
            const int maxWaitMs = 50;

            _logger.LogInformation(
                "PaymentProcessingJob iniciado. BatchSize: {BatchSize}, MaxParallelism: {MaxParallelism}, MaxWaitMs: {MaxWaitMs}",
                batchSize, maxParallelism, maxWaitMs);

            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider
                .GetRequiredService<IPaymentProcessingOrchestratorService>();

            var buffer = new List<NewPaymentRequest>(batchSize);
            var stopwatch = new Stopwatch();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Iniciando novo batch de processamento...");

                try
                {
                    buffer.Clear();
                    stopwatch.Restart();

                    var first = await _processingReader.ReadAsync(stoppingToken);
                    buffer.Add(first);

                    var batchStart = Stopwatch.GetTimestamp();
                    while (buffer.Count < batchSize)
                    {
                        var elapsed = batchStart.ElapsedMilliseconds();
                        if (elapsed >= maxWaitMs) break;

                        var readTask = _processingReader.WaitToReadAsync(stoppingToken).AsTask();
                        var delayTask = Task.Delay(maxWaitMs - (int)elapsed, stoppingToken);

                        var winner = await Task.WhenAny(readTask, delayTask);
                        if (winner == delayTask || !readTask.Result) break;

                        while (buffer.Count < batchSize && _processingReader.TryRead(out var item))
                            buffer.Add(item);
                    }

                    stopwatch.Restart();
                    await Parallel.ForEachAsync(buffer, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = maxParallelism,
                        CancellationToken = stoppingToken
                    }, async (req, ct) =>
                    {
                        var payment = new Payment
                        {
                            CorrelationId = req.CorrelationId,
                            Amount = req.Amount,
                            RequestedAt = DateTimeOffset.UtcNow
                        };

                        try
                        {
                            var result = await orchestrator.ProcessAsync(payment, ct)
                                .ConfigureAwait(false);

                            if (result.IsSuccess)
                            {
                                await _persistenceReader.WriteAsync(result.Content, ct)
                                    .ConfigureAwait(false);
                                return;
                            }

                            _logger.LogError("Pagamento falhou: {Id}", req.CorrelationId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro no pagamento {Id}.", payment.CorrelationId);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no main loop do batch!");
                }
            }
        }
    }
}