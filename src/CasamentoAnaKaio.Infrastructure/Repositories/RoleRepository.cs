using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;
using CasamentoAnaKaio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CasamentoAnaKaio.Infrastructure.Repositories;

public sealed class RoleRepository(AppDbContext context) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Roles
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await context.Roles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower(), cancellationToken);
    }

    public async Task<Role?> GetByTypeAsync(RoleType roleType, CancellationToken cancellationToken)
    {
        return await context.Roles
            .FirstOrDefaultAsync(r => r.RoleType == roleType, cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Roles.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken)
    {
        await context.Roles.AddAsync(role, cancellationToken);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken)
    {
        context.Roles.Update(role);
        await Task.CompletedTask;
    }
}
