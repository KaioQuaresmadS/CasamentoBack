using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Contracts.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasamentoAnaKaio.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthenticationController(
    IAuthenticationService authenticationService,
    ITokenService tokenService,
    IConfiguration configuration,
    IHostEnvironment environment) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (environment.IsDevelopment() && IsDevelopmentAdmin(request))
        {
            return Ok(CreateDevelopmentAdminResponse());
        }

        AuthenticationResult result;

        try
        {
            result = await authenticationService.LoginAsync(request.Email, request.Password, cancellationToken);
        }
        catch when (environment.IsDevelopment() && IsDevelopmentAdmin(request))
        {
            return Ok(CreateDevelopmentAdminResponse());
        }

        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.ErrorMessage });
        }

        return Ok(new LoginResponse(
            result.AccessToken!,
            result.RefreshToken!,
            result.UserId!,
            result.Email!,
            result.Name!,
            result.Roles ?? Array.Empty<string>()));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.ErrorMessage });
        }

        return Ok(new RefreshTokenResponse(
            result.AccessToken!,
            result.RefreshToken!));
    }

    private bool IsDevelopmentAdmin(LoginRequest request)
    {
        const string adminEmail = "kaio.ana";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_SEED_PASSWORD");

        return request.Email.Equals(adminEmail, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(adminPassword)
            && request.Password == adminPassword;
    }

    private LoginResponse CreateDevelopmentAdminResponse()
    {
        const string adminEmail = "kaio.ana";
        var adminName = configuration["AdminSeed:Name"] ?? "Admin Casamento Ana e Kaio";
        var userId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var roles = new[] { "Admin" };

        return new LoginResponse(
            tokenService.GenerateAccessToken(userId, adminEmail, roles),
            tokenService.GenerateRefreshToken(),
            userId.ToString(),
            adminEmail,
            adminName,
            roles);
    }
}
