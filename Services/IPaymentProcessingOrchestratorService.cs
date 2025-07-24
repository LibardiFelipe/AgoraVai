using AgoraVai.Entities;
using AgoraVai.Utils;

namespace AgoraVai.Services
{
    public interface IPaymentProcessingOrchestratorService
    {
        ValueTask<Result<Payment>> ProcessAsync(
            Payment payment, CancellationToken cancellationToken = default);
    }
}
