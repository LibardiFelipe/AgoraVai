using AgoraVai.WebAPI.Entities;
using AgoraVai.WebAPI.Utils;

namespace AgoraVai.WebAPI.Services
{
    public interface IPaymentProcessingOrchestratorService
    {
        ValueTask<Result<Payment>> ProcessAsync(
            Payment payment, CancellationToken cancellationToken = default);
    }
}
