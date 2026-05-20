namespace CasamentoAnaKaio.Contracts.Payments;

public sealed record CreatePaymentRequest(
    Guid GiftId,
    string PayerName,
    string? PayerEmail,
    string PayerPhone,
    string Mode,
    int QuotaQuantity,
    string? PaymentMethod);
