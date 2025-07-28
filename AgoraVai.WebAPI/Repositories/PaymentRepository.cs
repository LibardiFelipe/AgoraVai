using AgoraVai.WebAPI.Entities;
using StackExchange.Redis;
using System.Globalization;

namespace AgoraVai.WebAPI.Repositories
{
    public sealed class PaymentRepository : IPaymentRepository
    {
        private readonly IDatabase _database;
        private readonly IServer _server;

        public PaymentRepository(IConnectionMultiplexer multiplexer)
        {
            _database = multiplexer.GetDatabase();
            _server = multiplexer.GetServer(multiplexer.GetEndPoints()[0]);
        }

        public async ValueTask<SummariesReadModel> GetProcessorsSummaryAsync(
            DateTimeOffset? from, DateTimeOffset? to)
        {
            var fromTs = from?.ToUnixTimeMilliseconds() ?? 0;
            var toTs = to?.ToUnixTimeMilliseconds() ?? double.MaxValue;

            async Task<SummaryReadModel> SummaryForAsync(string processor)
            {
                var zkey = $"summary:{processor}:history";
                var hkey = $"summary:{processor}:data";

                var ids = await _database.SortedSetRangeByScoreAsync(zkey, fromTs, toTs)
                    .ConfigureAwait(false);

                if (ids.Length <= 0)
                    return new SummaryReadModel { TotalRequests = 0, TotalAmount = 0m };

                var values = await _database.HashGetAsync(hkey, ids)
                    .ConfigureAwait(false);

                var totalRequests = values.Length;
                var totalAmount = 0m;
                foreach (var v in values)
                {
                    if (v.IsNull)
                        continue;
                    if (decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                        totalAmount += dec;
                }

                return new SummaryReadModel
                {
                    TotalRequests = totalRequests,
                    TotalAmount = Math.Round(totalAmount, 2)
                };
            }

            var summary = new SummariesReadModel
            {
                Default = await SummaryForAsync("default"),
                Fallback = await SummaryForAsync("fallback")
            };

            return summary;
        }

        public async ValueTask<bool> InsertAsync(Payment payment)
        {
            var processor = payment.ProcessedBy;
            var hashKey = $"summary:{processor}:data";
            var zsetKey = $"summary:{processor}:history";

            var tran = _database.CreateTransaction();
            _ = tran.HashSetAsync(
                hashKey, payment.CorrelationId.ToString(), payment.Amount.ToString(CultureInfo.InvariantCulture));
            _ = tran.SortedSetAddAsync(
                zsetKey, payment.CorrelationId.ToString(), payment.RequestedAtUtc.ToUnixTimeMilliseconds());

            return await tran.ExecuteAsync();
        }

        public async ValueTask PurgeAsync()
        {
            var key = $"summary:*";
            const int pageSize = 250;

            var keys = new List<RedisKey>(pageSize);
            await foreach (var hashKey in _server.KeysAsync(
                pattern: $"{key}:data", pageSize: pageSize))
            {
                keys.Add(hashKey);
                if (keys.Count >= pageSize)
                {
                    await _database.KeyDeleteAsync([.. keys]);
                    keys.Clear();
                }
            }

            if (keys.Count > 0)
                await _database.KeyDeleteAsync([.. keys]);
        }
    }
}
