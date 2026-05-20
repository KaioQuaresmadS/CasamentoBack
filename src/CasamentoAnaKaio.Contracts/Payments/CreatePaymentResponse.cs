using System.Text.Json.Serialization;

namespace CasamentoAnaKaio.Contracts.Payments;

public sealed record CreatePaymentResponse(
    Guid Id,
    Guid GiftContributionId,
    string Status,
    string PaymentMethod,
    decimal Amount,
    string CheckoutUrl,
    string InitPoint,
    string SandboxInitPoint,
    string? PaymentUrl,
    string? TicketUrl,
    string? BoletoUrl,
    string? Barcode,
    string? QrCode,
    string? QrCodeBase64,
    string? PixCopyPaste,
    string PreferenceId,
    string? MercadoPagoPaymentId,
    string ExternalReference)
{
    [JsonPropertyName("init_point")]
    public string InitPointSnake => InitPoint;

    [JsonPropertyName("sandbox_init_point")]
    public string SandboxInitPointSnake => SandboxInitPoint;

    [JsonPropertyName("payment_url")]
    public string? PaymentUrlSnake => PaymentUrl;

    [JsonPropertyName("ticket_url")]
    public string? TicketUrlSnake => TicketUrl;

    [JsonPropertyName("boleto_url")]
    public string? BoletoUrlSnake => BoletoUrl;

    [JsonPropertyName("qr_code")]
    public string? QrCodeSnake => QrCode;

    [JsonPropertyName("qr_code_base64")]
    public string? QrCodeBase64Snake => QrCodeBase64;

    [JsonPropertyName("pix_copy_paste")]
    public string? PixCopyPasteSnake => PixCopyPaste;
}
