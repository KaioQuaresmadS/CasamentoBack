namespace CasamentoAnaKaio.Contracts.Gifts;

public sealed record UpdateGiftRequest(
    string Name,
    string Description,
    string ImageUrl,
    decimal Price,
    int ReservedPercent);
