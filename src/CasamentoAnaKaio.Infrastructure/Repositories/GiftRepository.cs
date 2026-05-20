using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CasamentoAnaKaio.Infrastructure.Repositories;

public sealed class GiftRepository(AppDbContext dbContext) : IGiftRepository
{
    public async Task<Gift?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Gifts.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<Gift>> ListActiveAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Gifts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
