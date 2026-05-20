namespace CasamentoAnaKaio.Contracts.Payments;

public sealed record PaymentStatusResponse(
    Guid Id,
    Guid GiftContributionId,
    string Status,
    string PaymentMethod,
    decimal Amount,
    DateTimeOffset? PaidAt);
