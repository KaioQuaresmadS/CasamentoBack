using CasamentoAnaKaio.Domain.Entities;

namespace CasamentoAnaKaio.Application.Abstractions;

public interface IGiftRepository
{
    Task<Gift?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Gift>> ListActiveAsync(CancellationToken cancellationToken);
    Task AddAsync(Gift gift, CancellationToken cancellationToken);
    Task UpdateAsync(Gift gift, CancellationToken cancellationToken);
}
