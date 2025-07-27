using AgoraVai.WebAPI.Entities;
using System.Text.Json.Serialization;

namespace AgoraVai.WebAPI.Repositories
{
    public sealed class SummariesReadModel
    {
        [JsonPropertyName("default")]
        public SummaryReadModel Default { get; init; } = new SummaryReadModel
        {
            TotalAmount = 0,
            TotalRequests = 0
        };

        [JsonPropertyName("fallback")]
        public SummaryReadModel Fallback { get; init; } = new SummaryReadModel
        {
            TotalAmount = 0,
            TotalRequests = 0
        };
    }

    public sealed class SummaryReadModel
    {
        [JsonPropertyName("total_requests")]
        public long TotalRequests { get; init; } = 0;
        
        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; init; } = 0;
    }

    public sealed class SummaryRowReadModel
    {
        public string ProcessedBy { get; init; } = string.Empty;
        public long TotalRequests { get; init; }
        public decimal TotalAmount { get; init; }
    }

    public interface IPaymentRepository
    {
        ValueTask InserBatchAsync(IEnumerable<Payment> payments);
        ValueTask<IEnumerable<SummaryRowReadModel>> GetProcessorsSummaryAsync(
            DateTimeOffset? from, DateTimeOffset? to);
        ValueTask PurgeAsync();
    }
}
