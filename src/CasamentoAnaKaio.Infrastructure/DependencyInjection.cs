using CasamentoAnaKaio.Application.Abstractions;
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
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IGuestConfirmationRepository, GuestConfirmationRepository>();
        services.AddScoped<IGiftRepository, GiftRepository>();
        services.AddScoped<IGiftContributionRepository, GiftContributionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPixPaymentService, MockPixPaymentService>();

        return services;
    }
}
