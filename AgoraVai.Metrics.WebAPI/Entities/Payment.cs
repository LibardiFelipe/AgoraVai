namespace AgoraVai.Metrics.WebAPI.Entities
{
    public sealed class Payment
    {
        public string Processor { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public DateTime Date { get; init; }
    }
}
