namespace CasamentoAnaKaio.Contracts.Payments;

public sealed record CreatePaymentResponse(
    Guid Id,
    Guid GiftContributionId,
    string PaymentStatus,
    string PaymentMethod,
    decimal Amount,
    string PreferenceId,
    string InitPoint,
    string SandboxInitPoint,
    DateTimeOffset CreatedAt);
