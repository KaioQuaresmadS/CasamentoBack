using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Contracts.Gifts;
using CasamentoAnaKaio.Domain.Entities;

namespace CasamentoAnaKaio.Application.Services;

public sealed class GiftService(IGiftRepository repository)
{
    public async Task<IReadOnlyList<GiftResponse>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var gifts = await repository.ListActiveAsync(cancellationToken);
        return gifts.Select(Map).ToList();
    }

    public async Task<GiftResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var gift = await repository.GetByIdAsync(id, cancellationToken);
        return gift is null ? null : Map(gift);
    }

    private static GiftResponse Map(Gift gift)
    {
        return new GiftResponse(
            gift.Id,
            gift.Name,
            gift.Description,
            gift.ImageUrl,
            gift.Price,
            gift.ReservedPercent);
    }
}
