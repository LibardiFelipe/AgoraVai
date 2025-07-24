using AgoraVai.Entities;

namespace AgoraVai.Services
{
    public interface IPaymentProcessorService
    {
        string ProcessorName { get; }
        ValueTask<bool> ProcessAsync(
            Payment payment, CancellationToken cancellationToken = default);
    }

    public interface IDefaultPaymentProcessorService : IPaymentProcessorService;
    public interface IFallbackPaymentProcessorService : IPaymentProcessorService;
}
