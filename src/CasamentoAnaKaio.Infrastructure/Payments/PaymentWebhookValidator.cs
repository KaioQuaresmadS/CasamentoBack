using System.Security.Cryptography;
using System.Text;
using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Infrastructure.Options;
using Microsoft.Extensions.Logging;

namespace CasamentoAnaKaio.Infrastructure.Payments;

public sealed class PaymentWebhookValidator(
    MercadoPagoOptions options,
    ILogger<PaymentWebhookValidator> logger) : IPaymentWebhookValidator
{
    public bool ValidateSignature(string dataId, string xRequestId, string xSignature)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
        {
            logger.LogWarning("MERCADOPAGO_WEBHOOK_SECRET nao configurado. Webhook aceito apenas para facilitar desenvolvimento local.");
            return true;
        }

        if (string.IsNullOrWhiteSpace(dataId)
            || string.IsNullOrWhiteSpace(xRequestId)
            || string.IsNullOrWhiteSpace(xSignature))
        {
            return false;
        }

        var parts = ParseSignatureParts(xSignature);
        if (!parts.TryGetValue("ts", out var ts) || !parts.TryGetValue("v1", out var receivedHash))
        {
            return false;
        }

        var manifest = $"id:{dataId};request-id:{xRequestId};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.WebhookSecret));
        var computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(receivedHash.ToLowerInvariant()));
    }

    private static Dictionary<string, string> ParseSignatureParts(string signature)
    {
        return signature
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);
    }
}
