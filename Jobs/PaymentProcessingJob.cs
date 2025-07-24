using AgoraVai.Channels;
using AgoraVai.Entities;
using AgoraVai.Requests;
using AgoraVai.Utils;
using System.Diagnostics;
using System.Threading.Channels;

namespace AgoraVai.Jobs
{
    public class PaymentProcessingJob : BackgroundService
    {
        private readonly ChannelReader<NewPaymentRequest> _processingReader;
        private readonly PersistenceChannel _persistenceReader;
        private readonly ILogger<PaymentProcessingJob> _logger;

        public PaymentProcessingJob(
            ProcessorChannel processorChannel,
            PersistenceChannel persistenceChannel,
            ILogger<PaymentProcessingJob> logger)
        {
            _processingReader = processorChannel.GetReader();
            _persistenceReader = persistenceChannel;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int batchSize = 100;
            const int maxParallelism = 10;
            const int maxWaitMs = 50;

            var buffer = new List<NewPaymentRequest>(batchSize);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    buffer.Clear();

                    var first = await _processingReader.ReadAsync(stoppingToken);
                    buffer.Add(first);

                    var batchStart = Stopwatch.GetTimestamp();
                    while (buffer.Count < batchSize)
                        while (buffer.Count < batchSize)
                        {
                            var elapsed = ElapsedMilliseconds(batchStart);
                            if (elapsed >= maxWaitMs) break;

                            var readTask = _processingReader.WaitToReadAsync(stoppingToken).AsTask();
                            var delayTask = Task.Delay(maxWaitMs - (int)elapsed, stoppingToken);

                            var winner = await Task.WhenAny(readTask, delayTask);
                            if (winner == delayTask || !readTask.Result) break;

                            while (buffer.Count < batchSize && _processingReader.TryRead(out var item))
                                buffer.Add(item);
                        }

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
                            ReceivedAt = DateTimeOffset.UtcNow
                        };

                        try
                        {
                            var result = await ProcessPaymentAsync(payment)
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
                    _logger.LogError(ex, "Erro no main loop!");
                }
            }
        }

        private static long ElapsedMilliseconds(long startTicks) =>
            (Stopwatch.GetTimestamp() - startTicks) * 1000 / Stopwatch.Frequency;

        private async Task<Result<Payment>> ProcessPaymentAsync(Payment payment)
        {
            await Task.Delay(5);
            payment.ChangeProcessedBy("proccessor");
            return Result<Payment>.Success(payment);
        }
    }
}