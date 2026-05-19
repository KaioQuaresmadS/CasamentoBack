namespace CasamentoAnaKaio.Domain.Entities;

public class GuestConfirmation
{
    private GuestConfirmation()
    {
    }

    public GuestConfirmation(string fullName, string phone, int guestsCount, bool willAttend, string? notes)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Nome completo e obrigatorio.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Celular e obrigatorio.", nameof(phone));
        }

        if (guestsCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(guestsCount), "Acompanhantes nao pode ser negativo.");
        }

        FullName = fullName.Trim();
        Phone = phone.Trim();
        GuestsCount = guestsCount;
        WillAttend = willAttend;
        Notes = notes?.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string FullName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public int GuestsCount { get; private set; }
    public bool WillAttend { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
