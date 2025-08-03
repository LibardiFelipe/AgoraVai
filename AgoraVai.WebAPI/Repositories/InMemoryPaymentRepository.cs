using AgoraVai.WebAPI.Entities;
using System.Collections.Concurrent;
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
        [JsonPropertyName("totalRequests")]
        public long TotalRequests { get; init; } = 0;

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; init; } = 0;
    }

    public sealed class SummaryRowReadModel
    {
        public string ProcessedBy { get; init; } = string.Empty;
        public long TotalRequests { get; init; }
        public decimal TotalAmount { get; init; }
    }

    public sealed class InMemoryPaymentRepository
    {
        private static readonly Lazy<InMemoryPaymentRepository> _instance =
            new(() => new InMemoryPaymentRepository());

        public static InMemoryPaymentRepository Instance => _instance.Value;
        private readonly ConcurrentBag<Payment> _payments = [];

        private InMemoryPaymentRepository() { }

        public void Insert(Payment payment) => _payments.Add(payment);

        public IEnumerable<SummaryRowReadModel> GetSummaries(
            DateTimeOffset? from, DateTimeOffset? to)
        {
            var filtered = _payments;

            if (from.HasValue || to.HasValue)
            {
                filtered = [.. _payments
                    .Where(p =>
                        (!from.HasValue || p.RequestedAt >= from.Value) &&
                        (!to.HasValue || p.RequestedAt <= to.Value))];
            }

            var summaries = filtered
                .GroupBy(p => p.ProcessedBy)
                .Select(g => new SummaryRowReadModel
                {
                    ProcessedBy = g.Key,
                    TotalRequests = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                });

            return summaries;
        }

        public void Purge() => _payments.Clear();
    }
}
