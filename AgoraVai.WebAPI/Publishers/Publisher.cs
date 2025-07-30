using AgoraVai.Shared.Configs;
using NetMQ;
using NetMQ.Sockets;

namespace AgoraVai.WebAPI.Publishers
{
    public sealed class Publisher : IDisposable
    {
        private readonly BrokerConfig _brokerConfig;
        private readonly PublisherSocket _publisher;

        public Publisher(BrokerConfig brokerConfig)
        {
            _brokerConfig = brokerConfig;

            _publisher = new PublisherSocket();
            _publisher.Connect(_brokerConfig.GetConnString());
        }

        public void PublishMessage(string message) =>
            _publisher.SendFrame($"{_brokerConfig.Topic} {message}");

        public void Dispose() =>
            _publisher?.Dispose();
    }
}
