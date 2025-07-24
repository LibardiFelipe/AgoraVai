namespace AgoraVai.Entities
{
    public sealed class Payment
    {
        public Guid CorrelationId { get; init; }
        public decimal Amount { get; init; }
        public DateTimeOffset ReceivedAt { get; init; }
        public string ProcessedBy { get; private set; } = string.Empty;

        public void ChangeProcessedBy(string processorName) =>
            ProcessedBy = processorName;
    }
}
