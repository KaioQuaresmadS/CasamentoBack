using CasamentoAnaKaio.Application.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace CasamentoAnaKaio.Infrastructure.Payments;

public sealed class PaymentWebhookValidator : IPaymentWebhookValidator
{
    private readonly string _webhookSecret;

    public PaymentWebhookValidator(string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret))
            throw new ArgumentException("Webhook secret não pode estar vazio.", nameof(webhookSecret));

        _webhookSecret = webhookSecret;
    }

    public bool ValidateSignature(string payload, string signature)
    {
        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature))
            return false;

        try
        {
            var expectedSignature = ComputeSignature(payload);
            return expectedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private string ComputeSignature(string payload)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }
    }
}
