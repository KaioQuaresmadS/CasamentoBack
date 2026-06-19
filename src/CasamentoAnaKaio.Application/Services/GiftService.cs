using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Contracts.Gifts;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;
using FluentValidation;

namespace CasamentoAnaKaio.Application.Services;

public sealed class GiftService(
    IGiftRepository repository,
    IUnitOfWork unitOfWork,
    IValidator<CreateGiftRequest> createValidator,
    IValidator<UpdateGiftRequest> updateValidator)
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

    public async Task<GiftResponse> CreateAsync(CreateGiftRequest request, CancellationToken cancellationToken)
    {
        // Centraliza a validação de entrada antes de criar a entidade de domínio.
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var gift = new Gift(
            request.Name,
            request.Description,
            request.ImageUrl,
            request.Price,
            request.ReservedPercent);

        await repository.AddAsync(gift, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(gift);
    }

    public async Task<GiftResponse?> UpdateAsync(Guid id, UpdateGiftRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var gift = await repository.GetByIdAsync(id, cancellationToken);
        if (gift is null)
        {
            return null;
        }

        gift.Update(
            request.Name,
            request.Description,
            request.ImageUrl,
            request.Price,
            request.ReservedPercent);

        await repository.UpdateAsync(gift, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(gift);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var gift = await repository.GetByIdAsync(id, cancellationToken);
        if (gift is null)
        {
            return false;
        }

        // Remocao logica para preservar historico de contribuicoes vinculadas.
        gift.Deactivate();
        await repository.UpdateAsync(gift, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static GiftResponse Map(Gift gift)
    {
        var paidAmount = gift.Contributions
            .Where(x => x.PaymentStatus == PaymentStatus.Paid)
            .Sum(x => x.Amount);
        var paidReservedPercent = CalculateReservedPercent(gift.Price, paidAmount);
        var reservedPercent = Math.Max(gift.ReservedPercent, paidReservedPercent);
        var isPurchased = paidReservedPercent >= 100;

        return new GiftResponse(
            gift.Id,
            gift.Name,
            gift.Description,
            gift.ImageUrl,
            gift.Price,
            reservedPercent,
            paidAmount,
            paidAmount,
            isPurchased,
            isPurchased ? "confirmed" : "pending");
    }

    private static int CalculateReservedPercent(decimal price, decimal paidAmount)
    {
        if (price <= 0 || paidAmount <= 0)
        {
            return 0;
        }

        var percent = (int)Math.Round(paidAmount / price * 100m, MidpointRounding.AwayFromZero);
        return Math.Clamp(percent, 0, 100);
    }
}
