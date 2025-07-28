using AgoraVai.WebAPI.Entities;
using Npgsql;
using NpgsqlTypes;

namespace AgoraVai.WebAPI.Repositories
{
    public sealed class PaymentRepository : IPaymentRepository
    {
        private readonly string _connString;

        public PaymentRepository(string connString)
        {
            _connString = connString;
        }

        public async ValueTask InserBatchAsync(IEnumerable<Payment> payments)
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync().ConfigureAwait(false);

            const string sql = @"
                COPY payments (
                    correlation_id,
                    amount,
                    processed_by,
                    requested_at_utc)
                FROM STDIN (FORMAT BINARY)";

            await using var writer = await conn.BeginBinaryImportAsync(sql)
                .ConfigureAwait(false);
            foreach (var payment in payments)
            {
                await writer.StartRowAsync().ConfigureAwait(false);
                await writer.WriteAsync(payment.CorrelationId).ConfigureAwait(false);
                await writer.WriteAsync(payment.Amount).ConfigureAwait(false);
                await writer.WriteAsync(payment.ProcessedBy).ConfigureAwait(false);
                await writer.WriteAsync(payment.RequestedAt).ConfigureAwait(false);
            }

            await writer.CompleteAsync().ConfigureAwait(false);
        }

        public async ValueTask<IEnumerable<SummaryRowReadModel>> GetProcessorsSummaryAsync(
            DateTimeOffset? from, DateTimeOffset? to)
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync().ConfigureAwait(false);

            const string sql = @"
                SELECT 
                    processed_by AS ProcessedBy, 
                    COUNT(*) AS TotalRequests, 
                    SUM(amount) AS TotalAmount
                FROM payments
                WHERE (@from IS NULL OR requested_at_utc >= @from)
                  AND (@to IS NULL OR requested_at_utc <= @to)
                GROUP BY processed_by;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.Add(new NpgsqlParameter("@from", NpgsqlDbType.TimestampTz)
            {
                Value = (object?)from ?? DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("@to", NpgsqlDbType.TimestampTz)
            {
                Value = (object?)to ?? DBNull.Value
            });

            var summaries = new List<SummaryRowReadModel>();
            await using var reader = await cmd.ExecuteReaderAsync()
                .ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                summaries.Add(new SummaryRowReadModel
                {
                    ProcessedBy = reader.GetString(0),
                    TotalRequests = reader.GetInt64(1),
                    TotalAmount = reader.GetDecimal(2)
                });
            }

            return summaries;
        }

        public async ValueTask PurgeAsync()
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync()
                .ConfigureAwait(false);

            const string sql = "TRUNCATE TABLE payments;";
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync()
                .ConfigureAwait(false);
        }
    }
}
