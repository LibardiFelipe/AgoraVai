using AgoraVai.WebAPI.Entities;

namespace AgoraVai.WebAPI.Repositories
{
    public sealed class SummariesReadModel
    {
        public SummaryReadModel Default { get; init; } = new();
        public SummaryReadModel Fallback { get; init; } = new();
    }

    public sealed class SummaryReadModel
    {
        public long TotalRequests { get; init; }
        public decimal TotalAmount { get; init; }
    }

    public sealed class SummaryRowReadModel
    {
        public string ProcessedBy { get; init; } = string.Empty;
        public long TotalRequests { get; init; }
        public decimal TotalAmount { get; init; }
    }

    public interface IPaymentRepository
    {
        ValueTask<bool> InsertAsync(Payment payment);
        ValueTask<SummariesReadModel> GetProcessorsSummaryAsync(
            DateTimeOffset? from, DateTimeOffset? to);
        ValueTask PurgeAsync();
    }
}
