using AgoraVai.Services;

namespace AgoraVai.WebAPI.Services
{
    public sealed class DefaultPaymentProcessorService
        : BasePaymentProcessorService, IDefaultPaymentProcessorService
    {
        public DefaultPaymentProcessorService(
            HttpClient httpClient, ILogger<DefaultPaymentProcessorService> logger)
            : base(httpClient, logger)
        {
        }

        public override string ProcessorName => "default";
    }
}
