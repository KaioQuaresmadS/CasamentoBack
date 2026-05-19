namespace CasamentoAnaKaio.Contracts.Authentication;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string UserId,
    string Email,
    string Name,
    IEnumerable<string> Roles);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken);
