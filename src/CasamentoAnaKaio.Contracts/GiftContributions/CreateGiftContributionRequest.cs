namespace CasamentoAnaKaio.Contracts.GiftContributions;

public sealed record CreateGiftContributionRequest(
    Guid GiftId,
    string ContributorName,
    string ContributorPhone,
    string Mode,
    int QuotaQuantity);
