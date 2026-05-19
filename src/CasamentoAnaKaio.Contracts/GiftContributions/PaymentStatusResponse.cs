namespace CasamentoAnaKaio.Contracts.GiftContributions;

public sealed record PaymentStatusResponse(
    Guid Id,
    string PaymentStatus,
    DateTimeOffset? PaidAt);
