using AgoraVai.WebAPI.Channels;
using AgoraVai.WebAPI.Entities;
using AgoraVai.WebAPI.Repositories;
using AgoraVai.WebAPI.Utils;
using System.Diagnostics;
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
            const int batchSize = 200;
            const int maxWaitMs = 40;

            _logger.LogInformation(
                "PaymentPersistingJob iniciado. BatchSize: {BatchSize}, MaxWaitMs: {MaxWaitMs}",
                batchSize, maxWaitMs);

            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

            var buffer = new List<Payment>(batchSize);
            var batchCounter = 0;
            var stopwatch = new Stopwatch();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Iniciando novo batch de persistência...");

                try
                {
                    buffer.Clear();
                    stopwatch.Restart();

                    var first = await _reader.ReadAsync(stoppingToken);
                    buffer.Add(first);

                    var batchStart = Stopwatch.GetTimestamp();
                    while (buffer.Count < batchSize)
                    {
                        var elapsed = batchStart.ElapsedMilliseconds();
                        if (elapsed >= maxWaitMs) break;

                        var readTask = _reader.WaitToReadAsync(stoppingToken).AsTask();
                        var delayTask = Task.Delay(maxWaitMs - (int)elapsed, stoppingToken);

                        var winner = await Task.WhenAny(readTask, delayTask);
                        if (winner == delayTask || !readTask.Result) break;

                        while (buffer.Count < batchSize && _reader.TryRead(out var item))
                            buffer.Add(item);
                    }

                    batchCounter++;
                    _logger.LogInformation(
                        "Persistindo batch #{BatchNumber}: {BatchSize} pagamentos coletados em {ElapsedMs}ms",
                        batchCounter, buffer.Count, stopwatch.ElapsedMilliseconds);

                    stopwatch.Restart();
                    await repository.InserBatchAsync(buffer);

                    _logger.LogInformation(
                        "Batch #{BatchNumber} persistido com sucesso em {ElapsedMs}ms. Total de pagamentos: {PaymentCount}",
                        batchCounter, stopwatch.ElapsedMilliseconds, buffer.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao persistir o batch #{BatchNumber}!", batchCounter);
                }
            }

            _logger.LogInformation("PaymentPersistingJob finalizado após processar {TotalBatches} batches", batchCounter);
        }
    }
}
