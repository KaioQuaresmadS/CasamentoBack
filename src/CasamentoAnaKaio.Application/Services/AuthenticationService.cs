using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Application.Security;

namespace CasamentoAnaKaio.Application.Services;

public sealed class AuthenticationService(
    IUserRepository userRepository,
    ITokenService tokenService,
    PasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IAuthenticationService
{
    public async Task<AuthenticationResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthenticationResult(false, null, null, "Email e senha são obrigatórios.");
        }

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return new AuthenticationResult(false, null, null, "Credenciais inválidas.");
        }

        if (!passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return new AuthenticationResult(false, null, null, "Credenciais inválidas.");
        }

        user.UpdateLastLogin();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var roles = user.Roles.Select(r => r.Name).ToList();
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshToken = tokenService.GenerateRefreshToken();

        return new AuthenticationResult(
            true,
            accessToken,
            refreshToken,
            null,
            user.Id.ToString(),
            user.Email,
            user.Name,
            roles);
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new AuthenticationResult(false, null, null, "Refresh token é obrigatório.");
        }

        // Implementar lógica de refresh token com armazenamento em banco de dados
        return new AuthenticationResult(false, null, null, "Refresh token expirado ou inválido.");
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult(false);

        return Task.FromResult(tokenService.ValidateToken(token));
    }
}
