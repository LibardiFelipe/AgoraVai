using AgoraVai.WebAPI.Entities;

namespace AgoraVai.WebAPI.Repositories
{
    public interface IPaymentRepository
    {
        ValueTask InserBatchAsync(IEnumerable<Payment> payments);
    }
}
