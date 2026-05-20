namespace CasamentoAnaKaio.Contracts.Payments;

public sealed record PaymentStatusResponse(
    Guid Id,
    Guid GiftContributionId,
    string PaymentStatus,
    string MercadoPagoStatus,
    DateTimeOffset? PaidAt,
    DateTimeOffset UpdatedAt);
