using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CasamentoAnaKaio.Infrastructure.Repositories;

public sealed class GuestConfirmationRepository(AppDbContext dbContext) : IGuestConfirmationRepository
{
    public async Task AddAsync(GuestConfirmation confirmation, CancellationToken cancellationToken)
    {
        await dbContext.GuestConfirmations.AddAsync(confirmation, cancellationToken);
    }

    public async Task<IReadOnlyList<GuestConfirmation>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.GuestConfirmations
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
