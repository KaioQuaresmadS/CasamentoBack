using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Infrastructure;
using CasamentoAnaKaio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendPolicy";

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<GuestConfirmationService>();
builder.Services.AddScoped<GiftService>();
builder.Services.AddScoped<GiftContributionService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<PaymentWebhookService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
            .ToArray();

        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            allowedOrigins =
            [
                "https://casamento-ana-kaio.netlify.app",
                "http://localhost:4200",
                "http://127.0.0.1:4200"
            ];
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        context.Response.StatusCode = exception is ArgumentException or ArgumentOutOfRangeException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        var problem = new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = context.Response.StatusCode == StatusCodes.Status400BadRequest
                ? "Requisicao invalida"
                : "Erro inesperado",
            Detail = exception?.Message
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors(FrontendCorsPolicy);
app.MapControllers();
app.MapHealthChecks("/health");

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
_ = Task.Run(async () =>
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

    try
    {
        using var scope = scopeFactory.CreateScope();
        logger.LogInformation("Applying database migrations in background.");

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        logger.LogInformation("Database migrations applied.");
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Database migration failed.");
    }
});

app.Logger.LogInformation("Application configured. Starting web host.");
app.Run();
