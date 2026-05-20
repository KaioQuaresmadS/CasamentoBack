using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;

namespace CasamentoAnaKaio.Application.Abstractions;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task<Role?> GetByTypeAsync(RoleType roleType, CancellationToken cancellationToken);
    Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Role role, CancellationToken cancellationToken);
    Task UpdateAsync(Role role, CancellationToken cancellationToken);
}
