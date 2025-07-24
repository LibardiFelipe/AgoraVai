using AgoraVai.Entities;
using System.Threading.Channels;

namespace AgoraVai.Channels
{
    public sealed class PersistenceChannel
    {
        private readonly Channel<Payment> _channel;

        public PersistenceChannel()
        {
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            };

            _channel = Channel.CreateUnbounded<Payment>(options);
        }

        public ValueTask WriteAsync(Payment result, CancellationToken cancellationToken = default) =>
            _channel.Writer.WriteAsync(result, cancellationToken);

        public ChannelReader<Payment> GetReader() => _channel.Reader;
    }
}
