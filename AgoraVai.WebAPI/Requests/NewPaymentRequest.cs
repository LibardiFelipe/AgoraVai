namespace AgoraVai.WebAPI.Requests
{
    public record NewPaymentRequest(
        Guid CorrelationId, decimal Amount);
}
