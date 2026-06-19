using System.Text;
using CasamentoAnaKaio.Application.Exceptions;
using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Application.Validators;
using CasamentoAnaKaio.Contracts.Authentication;
using CasamentoAnaKaio.Contracts.GiftContributions;
using CasamentoAnaKaio.Contracts.Gifts;
using CasamentoAnaKaio.Contracts.GuestConfirmations;
using CasamentoAnaKaio.Infrastructure;
using CasamentoAnaKaio.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
builder.Services.AddScoped<IValidator<CreateGuestConfirmationRequest>, CreateGuestConfirmationRequestValidator>();
builder.Services.AddScoped<IValidator<CreateGiftContributionRequest>, CreateGiftContributionRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<CreateGiftRequest>, CreateGiftRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateGiftRequest>, UpdateGiftRequestValidator>();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? string.Empty;
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CasamentoAnaKaio";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CasamentoAnaKaioClient";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

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

await ApplyDatabaseMigrationsAsync(app);

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        context.Response.StatusCode = exception switch
        {
            PaymentMethodUnavailableException => StatusCodes.Status409Conflict,
            ValidationException or ArgumentException or ArgumentOutOfRangeException or InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = context.Response.StatusCode switch
            {
                StatusCodes.Status400BadRequest => "Requisicao invalida",
                StatusCodes.Status409Conflict => "Metodo de pagamento indisponivel",
                _ => "Erro inesperado"
            },
            Detail = exception?.Message
        };

        if (exception is ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());
        }

        await context.Response.WriteAsJsonAsync(problem);
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Logger.LogInformation("Application configured. Starting web host.");
app.Run();

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

    try
    {
        logger.LogInformation("Starting database migration application.");

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();

        logger.LogInformation(
            "Pending database migrations found: Count={PendingMigrationCount}, Migrations={PendingMigrations}",
            pendingMigrations.Length,
            pendingMigrations);

        await dbContext.Database.MigrateAsync();
        await DatabaseSeeder.SeedDefaultAdminAsync(app.Services);

        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Error applying database migrations.");
        throw;
    }
}
