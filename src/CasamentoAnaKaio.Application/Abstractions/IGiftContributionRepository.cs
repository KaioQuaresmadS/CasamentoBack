using CasamentoAnaKaio.Domain.Entities;

namespace CasamentoAnaKaio.Application.Abstractions;

public interface IGiftContributionRepository
{
    Task AddAsync(GiftContribution contribution, CancellationToken cancellationToken);
    Task<GiftContribution?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<GiftContribution?> GetByProviderPaymentIdAsync(string providerPaymentId, CancellationToken cancellationToken);
}
