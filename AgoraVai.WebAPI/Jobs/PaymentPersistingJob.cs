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

        public PaymentPersistingJob(
            PersistenceChannel channel,
            IServiceProvider serviceProvider)
        {
            _reader = channel.GetReader();
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int batchSize = 200;
            const int maxWaitMs = 40;

            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

            var buffer = new List<Payment>(batchSize);
            while (!stoppingToken.IsCancellationRequested)
            {
                buffer.Clear();

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

                await repository.InserBatchAsync(buffer);
            }
        }
    }
}
