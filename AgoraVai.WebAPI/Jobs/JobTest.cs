using NetMQ;
using NetMQ.Sockets;
using System.Collections.Concurrent;

namespace AgoraVai.WebAPI.Jobs
{
    public class JobTest : BackgroundService
    {
        private readonly SubscriberSocket _subscriber;
        private readonly ConcurrentQueue<string> _messageQueue;
        private readonly SemaphoreSlim _batchSemaphore;
        private readonly int _batchSize = 100;
        private readonly TimeSpan _batchTimeout = TimeSpan.FromMilliseconds(50);

        public JobTest()
        {
            _subscriber = new SubscriberSocket();
            _subscriber.Connect("tcp://localhost:5556");
            _subscriber.Subscribe("messages");

            _messageQueue = new ConcurrentQueue<string>();
            _batchSemaphore = new SemaphoreSlim(1, 1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Thread para receber mensagens
            var receiveTask = Task.Run(() => ReceiveMessages(stoppingToken), stoppingToken);

            // Thread para processar em lote
            var processTask = Task.Run(() => ProcessBatch(stoppingToken), stoppingToken);

            await Task.WhenAll(receiveTask, processTask);
        }

        private void ReceiveMessages(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_subscriber.TryReceiveFrameString(TimeSpan.FromMilliseconds(100), out var message))
                    {
                        _messageQueue.Enqueue(message);

                        if (_messageQueue.Count >= _batchSize)
                        {
                            _batchSemaphore.Release();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao receber mensagem: {ex.Message}");
                }
            }
        }

        private async Task ProcessBatch(CancellationToken stoppingToken)
        {
            var batch = new List<string>();
            var timer = new System.Timers.Timer(_batchTimeout.TotalMilliseconds)
            {
                AutoReset = false
            };

            timer.Elapsed += (sender, e) => _batchSemaphore.Release();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _batchSemaphore.WaitAsync(stoppingToken);

                    // Coleta mensagens para o lote
                    while (batch.Count < _batchSize && _messageQueue.TryDequeue(out string message))
                    {
                        batch.Add(message);
                    }

                    if (batch.Count > 0)
                    {
                        // Processa o lote em memória
                        batch.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar lote: {ex.Message}");
                }
            }
        }
    }
}
