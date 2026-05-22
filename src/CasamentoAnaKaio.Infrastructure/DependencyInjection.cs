using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Application.Security;
using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Infrastructure.Options;
using CasamentoAnaKaio.Infrastructure.Payments;
using CasamentoAnaKaio.Infrastructure.Persistence;
using CasamentoAnaKaio.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CasamentoAnaKaio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = BuildPostgresConnectionString(configuration);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IGuestConfirmationRepository, GuestConfirmationRepository>();
        services.AddScoped<IGiftRepository, GiftRepository>();
        services.AddScoped<IGiftContributionRepository, GiftContributionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<PasswordHasher>();

        var jwtOptions = new JwtOptions
        {
            Secret = configuration["Jwt:Secret"] ?? string.Empty,
            Issuer = configuration["Jwt:Issuer"] ?? "CasamentoAnaKaio",
            Audience = configuration["Jwt:Audience"] ?? "CasamentoAnaKaioClient",
            AccessTokenExpirationMinutes = int.TryParse(configuration["Jwt:AccessTokenExpirationMinutes"], out var minutes)
                ? minutes
                : 15
        };

        services.AddSingleton(jwtOptions);
        services.AddSingleton<ITokenService>(_ => new JwtTokenGenerator(
            jwtOptions.Secret,
            jwtOptions.Issuer,
            jwtOptions.Audience,
            jwtOptions.AccessTokenExpirationMinutes));

        var mercadoPagoOptions = new MercadoPagoOptions
        {
            AccessToken = configuration["MercadoPago:AccessToken"] ?? string.Empty,
            PublicKey = configuration["MercadoPago:PublicKey"] ?? string.Empty,
            WebhookSecret = configuration["MercadoPago:WebhookSecret"] ?? string.Empty,
            Environment = configuration["MercadoPago:Environment"] ?? "sandbox",
            FrontendUrl = configuration["MercadoPago:FrontendUrl"] ?? "http://localhost:4200",
            BackendUrl = configuration["MercadoPago:BackendUrl"] ?? "http://localhost:5000"
        };
        ApplyMercadoPagoEnvironmentOverrides(mercadoPagoOptions);
        services.AddSingleton(mercadoPagoOptions);
        services.AddScoped<IMercadoPagoPaymentClient>(_ =>
            new MercadoPagoPaymentClient(
                new HttpClient { BaseAddress = new Uri("https://api.mercadopago.com/") },
                mercadoPagoOptions));
        services.AddSingleton<IPaymentWebhookValidator>(new PaymentWebhookValidator(mercadoPagoOptions));

        return services;
    }

    private static void ApplyMercadoPagoEnvironmentOverrides(MercadoPagoOptions options)
    {
        options.AccessToken = Environment.GetEnvironmentVariable("MERCADOPAGO_ACCESS_TOKEN") ?? options.AccessToken;
        options.PublicKey = Environment.GetEnvironmentVariable("MERCADOPAGO_PUBLIC_KEY") ?? options.PublicKey;
        options.WebhookSecret = Environment.GetEnvironmentVariable("MERCADOPAGO_WEBHOOK_SECRET") ?? options.WebhookSecret;
        options.FrontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? options.FrontendUrl;
        options.BackendUrl = Environment.GetEnvironmentVariable("BACKEND_URL") ?? options.BackendUrl;
        options.Environment = Environment.GetEnvironmentVariable("MERCADOPAGO_ENVIRONMENT") ?? options.Environment;
    }

    private static string BuildPostgresConnectionString(IConfiguration configuration)
    {
        var connectionString = FirstNotBlank(
            Environment.GetEnvironmentVariable("DATABASE_URL"),
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"),
            configuration.GetConnectionString("DefaultConnection"),
            configuration["ConnectionStrings:DefaultConnection"]);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection ou DATABASE_URL.");
        }

        return NormalizePostgresConnectionString(connectionString);
    }

    private static string NormalizePostgresConnectionString(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var credentials = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(credentials[0]);
        var password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty;
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        var port = uri.IsDefaultPort ? 5432 : uri.Port;

        return
            $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }

    private static string? FirstNotBlank(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
