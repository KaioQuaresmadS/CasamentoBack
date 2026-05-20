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
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure()));

        // Repositórios
        services.AddScoped<IGuestConfirmationRepository, GuestConfirmationRepository>();
        services.AddScoped<IGiftRepository, GiftRepository>();
        services.AddScoped<IGiftContributionRepository, GiftContributionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Segurança
        services.AddSingleton<PasswordHasher>();

        // JWT e Tokens
        var jwtOptions = new JwtOptions();
        configuration.GetSection("Jwt").Bind(jwtOptions);
        services.AddSingleton(jwtOptions);
        services.AddSingleton<ITokenService>(sp =>
            new JwtTokenGenerator(
                jwtOptions.Secret,
                jwtOptions.Issuer,
                jwtOptions.Audience,
                jwtOptions.AccessTokenExpirationMinutes));

        // Autenticação
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Pagamentos
        var mercadoPagoOptions = new MercadoPagoOptions();
        configuration.GetSection("MercadoPago").Bind(mercadoPagoOptions);
        ApplyMercadoPagoEnvironmentOverrides(mercadoPagoOptions);
        services.AddSingleton(mercadoPagoOptions);
        services.AddHttpClient<IMercadoPagoPaymentClient, MercadoPagoPaymentClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.mercadopago.com/");
        });
        services.AddScoped<IPixPaymentService>(sp =>
        {
            return mercadoPagoOptions.IsSandbox
                ? new MockPixPaymentService(configuration)
                : new MercadoPagoPixPaymentService(mercadoPagoOptions);
        });
        services.AddSingleton<IPaymentWebhookValidator, PaymentWebhookValidator>();

        // Webhook
        services.AddScoped<PaymentWebhookService>();

        return services;
    }

    private static void ApplyMercadoPagoEnvironmentOverrides(MercadoPagoOptions options)
    {
        options.AccessToken = Environment.GetEnvironmentVariable("MERCADOPAGO_ACCESS_TOKEN") ?? options.AccessToken;
        options.PublicKey = Environment.GetEnvironmentVariable("MERCADOPAGO_PUBLIC_KEY") ?? options.PublicKey;
        options.WebhookSecret = Environment.GetEnvironmentVariable("MERCADOPAGO_WEBHOOK_SECRET") ?? options.WebhookSecret;
        options.FrontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? options.FrontendUrl;
        options.BackendUrl = Environment.GetEnvironmentVariable("BACKEND_URL") ?? options.BackendUrl;
        var environment = Environment.GetEnvironmentVariable("MERCADOPAGO_ENVIRONMENT");
        if (!string.IsNullOrWhiteSpace(environment))
        {
            options.Environment = environment;
            options.IsSandbox = environment.Equals("sandbox", StringComparison.OrdinalIgnoreCase);
        }
    }
}
