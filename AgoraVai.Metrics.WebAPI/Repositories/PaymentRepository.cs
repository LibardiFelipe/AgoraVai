using AgoraVai.Metrics.WebAPI.Entities;

namespace AgoraVai.Metrics.WebAPI.Repositories
{
    public class PaymentStats
    {
        public Stat Default { get; set; } = new();
        public Stat Fallback { get; set; } = new();
    }

    public class Stat
    {
        public int TotalAmount { get; set; }
        public decimal TotalRequests { get; set; }
    }

    public sealed class PaymentRepository
    {
        private readonly List<Payment> _payments = [];

        public void Add(Payment payment) =>
            _payments.Add(payment);

        public PaymentStats GetSummary(DateTime? from, DateTime? to)
        {
            var filteredPayments = _payments;

            if (from.HasValue || to.HasValue)
            {
                filteredPayments = [.. _payments.Where(p =>
                    (!from.HasValue || p.Date >= from.Value) &&
                    (!to.HasValue || p.Date <= to.Value))];
            }

            var groupedStats = filteredPayments
                .GroupBy(p => p.Processor)
                .ToDictionary(
                    g => g.Key,
                    g => new Stat
                    {
                        TotalAmount = g.Count(),
                        TotalRequests = g.Sum(p => p.Amount)
                    });

            return new PaymentStats
            {
                Default = groupedStats.GetValueOrDefault("default", new Stat()),
                Fallback = groupedStats.GetValueOrDefault("fallback", new Stat())
            };
        }

        public void Purge() =>
            _payments.Clear();
    }
}
