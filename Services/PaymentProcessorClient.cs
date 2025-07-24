using AgoraVai.Requests;

namespace AgoraVai.Services
{
    public class PaymentProcessorClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymentProcessorClient> _logger;

        public PaymentProcessorClient(HttpClient httpClient, ILogger<PaymentProcessorClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> ProcessPaymentAsync(NewPaymentRequest payload, CancellationToken cancellationToken = default)
        {
            try
            {
                // O IHttpClientFactory já configurou o BaseAddress, timeouts e a política de retry.
                var response = await _httpClient.PostAsJsonAsync(
                    "v1/payments", payload, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Pagamento {PaymentId} processado com sucesso.", payload.CorrelationId);
                    return true;
                }

                // Log detalhado em caso de erro esperado da API (ex: 400 Bad Request)
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Falha ao processar pagamento {PaymentId}. Status: {StatusCode}. Resposta: {ErrorContent}",
                    payload.CorrelationId,
                    response.StatusCode,
                    errorContent);

                // Retornamos 'false' para erros que não devem ser tentados novamente (como dados inválidos).
                // A política de retry só atuará em erros 5xx ou de rede.
                return false;
            }
            catch (HttpRequestException ex)
            {
                // O Polly vai gerenciar os retries, mas logamos o erro final se todos falharem.
                _logger.LogError(ex, "Erro de rede final ao processar pagamento {PaymentId}.", payload.CorrelationId);
                throw; // Re-lança para que o chamador saiba que a operação falhou catastroficamente.
            }
        }
    }
}
