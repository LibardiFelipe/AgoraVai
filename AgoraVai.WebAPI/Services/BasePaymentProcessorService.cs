using AgoraVai.WebAPI.Entities;
using AgoraVai.WebAPI.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgoraVai.Services
{
    public abstract class BasePaymentProcessorService : IPaymentProcessorService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BasePaymentProcessorService> _logger;

        protected BasePaymentProcessorService(
            HttpClient httpClient, ILogger<BasePaymentProcessorService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public abstract string ProcessorName { get; }

        public async ValueTask<bool> ProcessAsync(
            Payment payment, CancellationToken cancellationToken = default)
        {
            try
            {
                using var content = new StringContent(
                    content: JsonSerializer.Serialize(
                        payment,
                        PaymentJsonContext.Default.Payment),
                    encoding: Encoding.UTF8,
                    mediaType: "application/json");

                using var result = await _httpClient.PostAsync(
                    "/payments", content, cancellationToken);

                return result.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    [JsonSerializable(typeof(Payment))]
    public partial class PaymentJsonContext : JsonSerializerContext
    {
    }
}
