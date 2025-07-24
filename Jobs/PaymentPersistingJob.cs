using AgoraVai.Channels;
using AgoraVai.Entities;
using System.Diagnostics;
using System.Threading.Channels;

namespace AgoraVai.Jobs
{
    public class PaymentPersistingJob : BackgroundService
    {
        private readonly ChannelReader<Payment> _reader;

        public PaymentPersistingJob(PersistenceChannel channel)
        {
            _reader = channel.GetReader();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int batchSize = 200;
            const int maxWaitMs = 40;

            var buffer = new List<Payment>(batchSize);
            while (!stoppingToken.IsCancellationRequested)
            {
                buffer.Clear();

                var first = await _reader.ReadAsync(stoppingToken);
                buffer.Add(first);
                
                var batchStart = Stopwatch.GetTimestamp();
                while (buffer.Count < batchSize)
                {
                    var elapsed = ElapsedMilliseconds(batchStart);
                    if (elapsed >= maxWaitMs) break;

                    var readTask = _reader.WaitToReadAsync(stoppingToken).AsTask();
                    var delayTask = Task.Delay(maxWaitMs - (int)elapsed, stoppingToken);

                    var winner = await Task.WhenAny(readTask, delayTask);
                    if (winner == delayTask || !readTask.Result) break;

                    while (buffer.Count < batchSize && _reader.TryRead(out var item))
                        buffer.Add(item);
                }

                await PersistAsync(buffer, stoppingToken);
            }
        }

        private async Task PersistAsync(List<Payment> results, CancellationToken ct)
        {
            await Task.Delay(3, ct);
        }

        private static long ElapsedMilliseconds(long startTicks) =>
            (Stopwatch.GetTimestamp() - startTicks) * 1000 / Stopwatch.Frequency;
    }
}
