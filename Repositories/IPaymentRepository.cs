using AgoraVai.Entities;

namespace AgoraVai.Repositories
{
    public interface IPaymentRepository
    {
        ValueTask InserBatchAsync(IEnumerable<Payment> payments);
    }
}
