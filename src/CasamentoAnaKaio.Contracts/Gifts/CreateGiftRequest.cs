namespace CasamentoAnaKaio.Contracts.Gifts;

public sealed record CreateGiftRequest(
    string Name,
    string Description,
    string ImageUrl,
    decimal Price,
    int ReservedPercent);
