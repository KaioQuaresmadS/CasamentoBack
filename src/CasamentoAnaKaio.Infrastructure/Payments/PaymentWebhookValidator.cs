using System.Security.Cryptography;
using System.Text;
using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Infrastructure.Options;

namespace CasamentoAnaKaio.Infrastructure.Payments;

public sealed class PaymentWebhookValidator(MercadoPagoOptions options) : IPaymentWebhookValidator
{
    public bool ValidateSignature(string dataId, string requestId, string signature)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
        {
            return options.IsSandbox;
        }

        if (string.IsNullOrWhiteSpace(dataId) ||
            string.IsNullOrWhiteSpace(requestId) ||
            string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var parts = signature
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(pair => pair.Length == 2)
            .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.OrdinalIgnoreCase);

        if (!parts.TryGetValue("ts", out var timestamp) ||
            !parts.TryGetValue("v1", out var receivedSignature))
        {
            return false;
        }

        var normalizedDataId = dataId.Any(char.IsLetter) ? dataId.ToLowerInvariant() : dataId;
        var manifest = $"id:{normalizedDataId};request-id:{requestId};ts:{timestamp};";
        var expectedSignature = ComputeHmacHex(manifest, options.WebhookSecret);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(receivedSignature));
    }

    private static string ComputeHmacHex(string manifest, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
    }
}
