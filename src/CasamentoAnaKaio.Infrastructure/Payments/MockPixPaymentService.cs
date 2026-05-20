using CasamentoAnaKaio.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CasamentoAnaKaio.Infrastructure.Payments;

public sealed class MockPixPaymentService(IConfiguration configuration) : IPixPaymentService
{
    public PixPayment CreatePayment(string description, decimal amount)
    {
        var pixKey = configuration["Pix:Key"] ?? "anaekaio@email.com";
        var providerPaymentId = $"mock-{Guid.NewGuid():N}";
        var payload = $"PIX Casamento Ana e Kaio | {description} | R$ {amount:0.00} | {providerPaymentId}";
        var qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=220x220&data={Uri.EscapeDataString(payload)}";

        return new PixPayment(pixKey, payload, qrCodeUrl, providerPaymentId);
    }
}
