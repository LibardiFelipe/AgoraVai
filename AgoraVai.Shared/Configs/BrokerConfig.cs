namespace AgoraVai.Shared.Configs
{
    public sealed class BrokerConfig
    {
        public string Host { get; init; } = string.Empty;
        public int Port { get; init; }
        public string Topic { get; init; } = string.Empty;

        public string GetConnString() =>
            $"tcp://{Host}:{Port}";
    }
}
