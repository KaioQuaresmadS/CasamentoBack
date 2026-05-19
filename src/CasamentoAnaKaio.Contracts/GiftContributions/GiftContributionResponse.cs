namespace CasamentoAnaKaio.Contracts.GiftContributions;

public sealed record GiftContributionResponse(
    Guid Id,
    Guid GiftId,
    string GiftName,
    string ContributorName,
    string ContributorPhone,
    string Mode,
    int QuotaQuantity,
    decimal Amount,
    string PaymentStatus,
    string PixKey,
    string QrCodePayload,
    string QrCodeUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt);
