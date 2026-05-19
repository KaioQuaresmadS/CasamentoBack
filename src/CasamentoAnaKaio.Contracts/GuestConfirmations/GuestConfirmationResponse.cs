namespace CasamentoAnaKaio.Contracts.GuestConfirmations;

public sealed record GuestConfirmationResponse(
    Guid Id,
    string FullName,
    string Phone,
    int GuestsCount,
    bool WillAttend,
    string? Notes,
    DateTimeOffset CreatedAt);
