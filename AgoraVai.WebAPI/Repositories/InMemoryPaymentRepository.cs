using AgoraVai.WebAPI.Entities;

namespace AgoraVai.WebAPI.Repositories
{
    public sealed class InMemoryPaymentRepository : IPaymentRepository
    {
        private readonly List<Payment> _payments = [];

        public ValueTask InserBatchAsync(IEnumerable<Payment> payments)
        {
            _payments.AddRange(payments ?? []);
            return ValueTask.CompletedTask;
        }

        public ValueTask<IEnumerable<SummaryRowReadModel>> GetProcessorsSummaryAsync(
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

            return ValueTask.FromResult(summaries);
        }

        public ValueTask PurgeAsync()
        {
            _payments.Clear();
            return ValueTask.CompletedTask;
        }
    }
}
