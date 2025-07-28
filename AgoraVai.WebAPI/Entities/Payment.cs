namespace AgoraVai.WebAPI.Entities
{
    public sealed class Payment
    {
        public Guid CorrelationId { get; init; }
        public decimal Amount { get; init; }
        public DateTimeOffset RequestedAtUtc { get; init; }
        public string ProcessedBy { get; private set; } = string.Empty;

        public Payment WithProcessor(string processorName)
        {
            ProcessedBy = processorName;
            return this;
        }
    }
}
