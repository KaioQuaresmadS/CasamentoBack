namespace CasamentoAnaKaio.Domain.Enums;

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Expired = 4,
    Processing = 5,
    Cancelled = 6,
    Refunded = 7,
    ChargedBack = 8,
    Unknown = 9
}
