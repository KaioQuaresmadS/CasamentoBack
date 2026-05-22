using CasamentoAnaKaio.Application.Security;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CasamentoAnaKaio.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedDefaultAdminAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        var adminEmail = configuration["AdminSeed:Email"] ?? "kaio.ana";
        var adminName = configuration["AdminSeed:Name"] ?? "Admin Casamento Ana e Kaio";
        var adminPassword = configuration["AdminSeed:Password"]
            ?? Environment.GetEnvironmentVariable("ADMIN_SEED_PASSWORD");

        await EnsureRoleAsync(context, "Admin", RoleType.Admin, "Acesso administrativo completo.", cancellationToken);
        await EnsureRoleAsync(context, "User", RoleType.User, "Usuario autenticado.", cancellationToken);
        await EnsureRoleAsync(context, "Guest", RoleType.Guest, "Convidado com acesso publico.", cancellationToken);

        var admin = await context.Users
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Email == adminEmail.ToLowerInvariant(), cancellationToken);

        if (admin is null)
        {
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning("Admin seed skipped because AdminSeed:Password was not configured.");
                return;
            }

            admin = new User(adminEmail, adminName, passwordHasher.HashPassword(adminPassword));
            var adminRole = await context.Roles.SingleAsync(x => x.RoleType == RoleType.Admin, cancellationToken);
            admin.AddRole(adminRole);

            await context.Users.AddAsync(admin, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Default admin user seeded: {Email}", adminEmail);
            return;
        }

        if (!admin.HasRole("Admin"))
        {
            var adminRole = await context.Roles.SingleAsync(x => x.RoleType == RoleType.Admin, cancellationToken);
            admin.AddRole(adminRole);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureRoleAsync(
        AppDbContext context,
        string name,
        RoleType roleType,
        string description,
        CancellationToken cancellationToken)
    {
        if (await context.Roles.AnyAsync(x => x.RoleType == roleType, cancellationToken))
        {
            return;
        }

        await context.Roles.AddAsync(new Role(name, roleType, description), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
