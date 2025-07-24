using AgoraVai.Entities;
using Npgsql;

namespace AgoraVai.Repositories
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
            await conn.OpenAsync();

            const string sql = @"
                COPY payments (
                    correlation_id,
                    amount,
                    processed_by,
                    requested_at_utc)
                FROM STDIN (FORMAT BINARY)";

            await using var writer = await conn.BeginBinaryImportAsync(sql);
            foreach (var payment in payments)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(payment.CorrelationId);
                await writer.WriteAsync(payment.Amount);
                await writer.WriteAsync(payment.ProcessedBy);
                await writer.WriteAsync(payment.RequestedAt);
            }

            await writer.CompleteAsync();
        }
    }
}
