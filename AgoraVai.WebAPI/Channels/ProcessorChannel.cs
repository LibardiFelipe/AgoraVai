using AgoraVai.WebAPI.Requests;
using System.Threading.Channels;

namespace AgoraVai.WebAPI.Channels
{
    public sealed class ProcessorChannel
    {
        private readonly Channel<NewPaymentRequest> _channel;

        public ProcessorChannel()
        {
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            };

            _channel = Channel.CreateUnbounded<NewPaymentRequest>(options);
        }

        public ValueTask WriteAsync(NewPaymentRequest data, CancellationToken cancellationToken = default) =>
            _channel.Writer.WriteAsync(data, cancellationToken);

        public ChannelReader<NewPaymentRequest> GetReader() => _channel.Reader;
    }
}
