using AgoraVai.Services;

namespace AgoraVai.WebAPI.Services
{
    public sealed class DefaultPaymentProcessorService
        : BasePaymentProcessorService
    {
        public DefaultPaymentProcessorService(HttpClient httpClient)
            : base(httpClient)
        {
        }

        public override string ProcessorName => "default";
    }
}
