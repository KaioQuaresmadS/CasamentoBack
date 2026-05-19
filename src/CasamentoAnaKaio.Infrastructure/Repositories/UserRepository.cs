using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CasamentoAnaKaio.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == email.ToLower(), cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Users
            .Include(u => u.Roles)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.AddAsync(user, cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Update(user);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetByIdAsync(id, cancellationToken);
        if (user is not null)
        {
            context.Users.Remove(user);
        }
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken)
    {
        return await context.Users.AnyAsync(u => u.Email == email.ToLower(), cancellationToken);
    }
}
