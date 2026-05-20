namespace CasamentoAnaKaio.Contracts.Payments;

public sealed record CreatePaymentResponse(
    Guid Id,
    Guid GiftContributionId,
    string Status,
    string PaymentMethod,
    decimal Amount,
    string CheckoutUrl,
    string PreferenceId,
    string? MercadoPagoPaymentId,
    string ExternalReference);
