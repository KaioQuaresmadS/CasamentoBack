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

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(AngularCorsPolicy);
app.MapControllers();

app.Run();
