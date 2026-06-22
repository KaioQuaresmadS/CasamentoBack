using System.Text.Json.Serialization;

namespace CasamentoAnaKaio.Contracts.Payments;

public sealed record CreatePixPaymentRequest(
    Guid GiftId,
    string PayerName,
    string? PayerEmail,
    decimal Amount);

public sealed record CreatePixPaymentResponse(
    [property: JsonPropertyName("payment_id")] string PaymentId,
    string Status,
    [property: JsonPropertyName("qr_code")] string QrCode,
    [property: JsonPropertyName("qr_code_base64")] string QrCodeBase64,
    [property: JsonPropertyName("ticket_url")] string TicketUrl,
    [property: JsonPropertyName("external_reference")] string ExternalReference);
