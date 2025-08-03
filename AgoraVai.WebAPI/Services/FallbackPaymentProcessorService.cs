using AgoraVai.Services;

namespace AgoraVai.WebAPI.Services
{
    public sealed class FallbackPaymentProcessorService
        : BasePaymentProcessorService
    {
        public FallbackPaymentProcessorService(HttpClient httpClient)
            : base(httpClient)
        {
        }

        public override string ProcessorName => "fallback";
    }
}
