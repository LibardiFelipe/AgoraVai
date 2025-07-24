using AgoraVai.Services;

namespace AgoraVai.WebAPI.Services
{
    public sealed class FallbackPaymentProcessorService
        : BasePaymentProcessorService, IFallbackPaymentProcessorService
    {
        public FallbackPaymentProcessorService(
            HttpClient httpClient, ILogger<FallbackPaymentProcessorService> logger)
            : base(httpClient, logger)
        {
        }

        public override string ProcessorName => "fallback";
    }
}
