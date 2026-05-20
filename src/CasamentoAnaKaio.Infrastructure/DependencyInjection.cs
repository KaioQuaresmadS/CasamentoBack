using CasamentoAnaKaio.Application.Abstractions;
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
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IGuestConfirmationRepository, GuestConfirmationRepository>();
        services.AddScoped<IGiftRepository, GiftRepository>();
        services.AddScoped<IGiftContributionRepository, GiftContributionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

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
}
