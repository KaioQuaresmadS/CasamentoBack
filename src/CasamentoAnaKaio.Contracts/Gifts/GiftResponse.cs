namespace CasamentoAnaKaio.Contracts.Gifts;

public sealed record GiftResponse(
    Guid Id,
    string Name,
    string Description,
    string ImageUrl,
    decimal Price,
    int ReservedPercent);
