using NetMQ;
using NetMQ.Sockets;
using System.Text.Json;

namespace AgoraVai.WebAPI.Publishers
{
    public sealed class Publisher
    {
        private readonly PublisherSocket _publisher;
        private readonly string _topic = "messages";

        public Publisher()
        {
            _publisher = new PublisherSocket();
            _publisher.Bind("tcp://*:5556");
        }

        public void PublishMessage<T>(T message)
        {
            var jsonMessage = JsonSerializer.Serialize(message);
            _publisher.SendMoreFrame(_topic).SendFrame(jsonMessage);
        }

        public void PublishBatch<T>(IEnumerable<T> messages)
        {
            foreach (var message in messages)
            {
                var jsonMessage = JsonSerializer.Serialize(message);
                _publisher.SendMoreFrame(_topic).SendFrame(jsonMessage);
            }
        }
    }
}
