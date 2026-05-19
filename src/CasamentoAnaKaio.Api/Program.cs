using CasamentoAnaKaio.Api.Middleware;
using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Application.Validators;
using CasamentoAnaKaio.Infrastructure;
using CasamentoAnaKaio.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

const string AngularCorsPolicy = "AngularCors";

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort) &&
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/casamento-api-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Casamento Ana e Kaio API",
            Version = "v1",
            Description = "API para lista de presentes, confirmações de presença, autenticação administrativa JWT e pagamentos PIX."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Informe o token JWT no formato: Bearer {token}"
        };

        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks();

builder.Services
    .AddFluentValidationAutoValidation()
    .AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<GuestConfirmationService>();
builder.Services.AddScoped<GiftService>();
builder.Services.AddScoped<GiftContributionService>();
builder.Services.AddScoped<PaymentWebhookService>();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:4200", "http://127.0.0.1:4200" };

    options.AddPolicy(AngularCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT Secret not configured or too short. Configure 'Jwt:Secret' with at least 32 characters.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CasamentoAnaKaio";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CasamentoAnaKaioClient";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(new
                {
                    error = "Unauthorized",
                    message = "Token inválido ou expirado."
                });
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<LoggingMiddleware>();

app.MapOpenApi();

// UseHttpsRedirection removido — o Render gerencia HTTPS no proxy

app.UseCors(AngularCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

try
{
    if (app.Environment.IsDevelopment())
    {
        try
        {
            await DatabaseSeeder.SeedDefaultAdminAsync(app.Services);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Seed de desenvolvimento ignorado porque o banco local nao esta acessivel.");
        }
    }
    else
    {
        await DatabaseSeeder.SeedDefaultAdminAsync(app.Services);
    }

    Log.Information("Iniciando aplicação Casamento Ana e Kaio");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação encerrada inesperadamente");
}
finally
{
    await Log.CloseAndFlushAsync();
}
