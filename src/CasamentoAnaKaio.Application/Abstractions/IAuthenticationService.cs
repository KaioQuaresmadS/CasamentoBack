namespace CasamentoAnaKaio.Application.Abstractions;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<bool> ValidateTokenAsync(string token);
}

public sealed record AuthenticationResult(
    bool IsSuccess,
    string? AccessToken,
    string? RefreshToken,
    string? ErrorMessage,
    string? UserId = null,
    string? Email = null,
    string? Name = null,
    IReadOnlyList<string>? Roles = null);
