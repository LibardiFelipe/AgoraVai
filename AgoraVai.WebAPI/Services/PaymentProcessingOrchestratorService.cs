using AgoraVai.WebAPI.Entities;
using AgoraVai.WebAPI.Utils;

namespace AgoraVai.WebAPI.Services
{
    public sealed class PaymentProcessingOrchestratorService : IPaymentProcessingOrchestratorService
    {
        private readonly IDefaultPaymentProcessorService _defaultProcessor;
        private readonly IFallbackPaymentProcessorService _fallbackProcessor;

        public PaymentProcessingOrchestratorService(
            IDefaultPaymentProcessorService defaultProcessor,
            IFallbackPaymentProcessorService fallbackProcessor)
        {
            _defaultProcessor = defaultProcessor;
            _fallbackProcessor = fallbackProcessor;
        }

        public async ValueTask<Result<Payment>> ProcessAsync(
            Payment payment, CancellationToken cancellationToken = default)
        {
            var success = await _defaultProcessor.ProcessAsync(payment, cancellationToken);
            if (success)
            {
                return Result<Payment>.Success(
                    payment.WithProcessor(_defaultProcessor.ProcessorName));
            }

            success = await _fallbackProcessor.ProcessAsync(payment, cancellationToken);
            if (success)
            {
                return Result<Payment>.Success(
                    payment.WithProcessor(_fallbackProcessor.ProcessorName));
            }

            return Result<Payment>.Failure();
        }
    }
}
