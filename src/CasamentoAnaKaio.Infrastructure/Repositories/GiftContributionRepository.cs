using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CasamentoAnaKaio.Infrastructure.Repositories;

public sealed class GiftContributionRepository(AppDbContext dbContext) : IGiftContributionRepository
{
    public async Task AddAsync(GiftContribution contribution, CancellationToken cancellationToken)
    {
        await dbContext.GiftContributions.AddAsync(contribution, cancellationToken);
    }

    public async Task<GiftContribution?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.GiftContributions
            .Include(x => x.Gift)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<GiftContribution?> GetByProviderPaymentIdAsync(string providerPaymentId, CancellationToken cancellationToken)
    {
        return await dbContext.GiftContributions
            .Include(x => x.Gift)
            .FirstOrDefaultAsync(x => x.ProviderPaymentId == providerPaymentId, cancellationToken);
    }
}
