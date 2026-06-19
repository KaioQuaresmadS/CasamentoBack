using System.Text.Json.Serialization;

namespace CasamentoAnaKaio.Contracts.Payments;

public sealed record CreateBoletoPaymentResponse(
    Guid Id,
    Guid GiftContributionId,
    [property: JsonPropertyName("payment_id")] string PaymentId,
    string Status,
    [property: JsonPropertyName("ticket_url")] string? TicketUrl,
    [property: JsonPropertyName("boleto_url")] string? BoletoUrl,
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("linha_digitavel")] string? LinhaDigitavel,
    [property: JsonPropertyName("external_reference")] string ExternalReference,
    string? CheckoutUrl,
    [property: JsonPropertyName("payment_url")] string? PaymentUrl);
