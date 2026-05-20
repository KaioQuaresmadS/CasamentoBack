namespace CasamentoAnaKaio.Contracts.Payments;

public sealed record CreatePaymentRequest(
    Guid GiftId,
    string PayerName,
    string PayerEmail,
    string? PayerPhone,
    string PaymentMethod,
    string Mode,
    int QuotaQuantity);
