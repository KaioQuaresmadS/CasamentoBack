using System.Security.Claims;
using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Application.Security;
using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;

namespace CasamentoAnaKaio.Tests;

public sealed class AuthenticationServiceTests
{
    private readonly PasswordHasher _passwordHasher = new();

    [Fact]
    public async Task LoginAsync_WithValidAdminCredentials_ReturnsTokenAndRoles()
    {
        var admin = new User("admin@casamento.local", "Admin", _passwordHasher.HashPassword("Admin@123456"));
        admin.AddRole(new Role("Admin", RoleType.Admin));

        var tokenService = new FakeTokenService();
        var unitOfWork = new FakeUnitOfWork();
        var service = new AuthenticationService(
            new FakeUserRepository(admin),
            tokenService,
            _passwordHasher,
            unitOfWork);

        var result = await service.LoginAsync("ADMIN@CASAMENTO.LOCAL", "Admin@123456", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal("admin@casamento.local", result.Email);
        Assert.Contains("Admin", result.Roles!);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Equal(admin.Id, tokenService.LastUserId);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsInvalidCredentials()
    {
        var admin = new User("admin@casamento.local", "Admin", _passwordHasher.HashPassword("Admin@123456"));
        var service = new AuthenticationService(
            new FakeUserRepository(admin),
            new FakeTokenService(),
            _passwordHasher,
            new FakeUnitOfWork());

        var result = await service.LoginAsync("admin@casamento.local", "wrong-password", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.AccessToken);
        Assert.Equal("Credenciais inválidas.", result.ErrorMessage);
    }

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task AddAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(user?.Email == email.ToLowerInvariant());
        public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IEnumerable<User>>(user is null ? [] : [user]);
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(user?.Email == email.ToLowerInvariant() ? user : null);
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(user?.Id == id ? user : null);
        public Task UpdateAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public Guid LastUserId { get; private set; }

        public string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles)
        {
            LastUserId = userId;
            return "access-token";
        }

        public string GenerateRefreshToken() => "refresh-token";
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token) => null;
        public bool ValidateToken(string token) => token == "access-token";
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }
}
