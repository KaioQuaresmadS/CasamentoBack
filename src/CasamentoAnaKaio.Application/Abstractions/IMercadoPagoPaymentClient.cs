namespace CasamentoAnaKaio.Application.Abstractions;

public interface IMercadoPagoPaymentClient
{
    Task<MercadoPagoPreferenceResult> CreateCheckoutPreferenceAsync(
        MercadoPagoPreferenceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<MercadoPagoPaymentDetails> GetPaymentAsync(
        string mercadoPagoPaymentId,
        CancellationToken cancellationToken);
}

public sealed record MercadoPagoPreferenceRequest(
    string Title,
    decimal Amount,
    string PayerName,
    string PayerEmail,
    string PaymentMethod,
    string ExternalReference);

public sealed record MercadoPagoPreferenceResult(
    string Id,
    string InitPoint,
    string SandboxInitPoint);

public sealed record MercadoPagoPaymentDetails(
    string Id,
    string Status,
    string? ExternalReference,
    string? PaymentMethodId,
    string? PaymentTypeId,
    string? QrCode,
    string? QrCodeBase64,
    string? TicketUrl);
