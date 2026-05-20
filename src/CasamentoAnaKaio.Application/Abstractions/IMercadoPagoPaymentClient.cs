namespace CasamentoAnaKaio.Application.Abstractions;

public interface IMercadoPagoPaymentClient
{
    Task<MercadoPagoPreferenceResult> CreatePreferenceAsync(
        MercadoPagoPreferenceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken cancellationToken);
}

public sealed record MercadoPagoPreferenceRequest(
    string ExternalReference,
    string Title,
    decimal Amount,
    string PayerName,
    string PayerEmail,
    string PaymentMethod,
    string SuccessUrl,
    string FailureUrl,
    string PendingUrl,
    string NotificationUrl);

public sealed record MercadoPagoPreferenceResult(
    string PreferenceId,
    string InitPoint,
    string SandboxInitPoint);

public sealed record MercadoPagoPaymentDetails(
    string PaymentId,
    string Status,
    string ExternalReference,
    string PaymentMethodId,
    decimal Amount,
    string? PixQrCode,
    string? PixCopyPaste);
