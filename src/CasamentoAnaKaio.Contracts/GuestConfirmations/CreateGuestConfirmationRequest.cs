namespace CasamentoAnaKaio.Contracts.GuestConfirmations;

public sealed record CreateGuestConfirmationRequest(
    string FullName,
    string Phone,
    int GuestsCount,
    bool WillAttend,
    string? Notes);
