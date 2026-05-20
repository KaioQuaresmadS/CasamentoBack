using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Infrastructure;
using CasamentoAnaKaio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string AngularCorsPolicy = "AngularCors";

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<GuestConfirmationService>();
builder.Services.AddScoped<GiftService>();
builder.Services.AddScoped<GiftContributionService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<PaymentWebhookService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

        try
        {
            logger.LogInformation("Applying database migrations.");
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to apply database migrations.");
        }
    });
});

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

app.UseCors(AngularCorsPolicy);
app.MapControllers();

app.Run();
