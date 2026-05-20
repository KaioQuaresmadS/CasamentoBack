using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Contracts.GiftContributions;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;

namespace CasamentoAnaKaio.Application.Services;

public sealed class GiftContributionService(
    IGiftContributionRepository contributionRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<GiftContributionResponse> CreateAsync(
        CreateGiftContributionRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("Use POST /api/payments/create. Mercado Pago e o fluxo unico de pagamento.");
    }

    public async Task<GiftContributionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var contribution = await contributionRepository.GetByIdAsync(id, cancellationToken);
        return contribution is null ? null : Map(contribution, BuildQrCodeUrl(contribution.QrCodePayload));
    }

    public async Task<PaymentStatusResponse?> GetStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        var contribution = await contributionRepository.GetByIdAsync(id, cancellationToken);
        return contribution is null
            ? null
            : new PaymentStatusResponse(contribution.Id, contribution.PaymentStatus.ToString(), contribution.PaidAt);
    }

    public async Task<PaymentStatusResponse?> MarkAsPaidAsync(Guid id, CancellationToken cancellationToken)
    {
        var contribution = await contributionRepository.GetByIdAsync(id, cancellationToken);
        if (contribution is null)
        {
            return null;
        }

        contribution.MarkAsPaid();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new PaymentStatusResponse(contribution.Id, contribution.PaymentStatus.ToString(), contribution.PaidAt);
    }

    public async Task<PaymentStatusResponse?> MarkAsFailedAsync(Guid id, CancellationToken cancellationToken)
    {
        var contribution = await contributionRepository.GetByIdAsync(id, cancellationToken);
        if (contribution is null)
        {
            return null;
        }

        contribution.MarkAsFailed();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new PaymentStatusResponse(contribution.Id, contribution.PaymentStatus.ToString(), contribution.PaidAt);
    }

    private static GiftContributionResponse Map(GiftContribution contribution, string qrCodeUrl)
    {
        return new GiftContributionResponse(
            contribution.Id,
            contribution.GiftId,
            contribution.Gift?.Name ?? string.Empty,
            contribution.ContributorName,
            contribution.ContributorPhone,
            contribution.Mode.ToString(),
            contribution.QuotaQuantity,
            contribution.Amount,
            contribution.PaymentStatus.ToString(),
            contribution.PixKey,
            contribution.QrCodePayload,
            qrCodeUrl,
            contribution.CreatedAt,
            contribution.PaidAt);
    }

    private static string BuildQrCodeUrl(string payload)
    {
        return $"https://api.qrserver.com/v1/create-qr-code/?size=220x220&data={Uri.EscapeDataString(payload)}";
    }
}
