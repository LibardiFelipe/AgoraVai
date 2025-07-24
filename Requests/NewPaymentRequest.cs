namespace AgoraVai.Requests
{
    public record NewPaymentRequest(
        Guid CorrelationId, decimal Amount);
}
