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
        services.AddSingleton(mercadoPagoOptions);
        services.AddScoped<IPixPaymentService>(sp =>
        {
            return mercadoPagoOptions.IsSandbox
                ? new MockPixPaymentService(configuration)
                : new MercadoPagoPixPaymentService(mercadoPagoOptions);
        });
        services.AddSingleton<IPaymentWebhookValidator>(new PaymentWebhookValidator(mercadoPagoOptions.WebhookSecret));

        // Webhook
        services.AddScoped<PaymentWebhookService>();

        return services;
    }
}
