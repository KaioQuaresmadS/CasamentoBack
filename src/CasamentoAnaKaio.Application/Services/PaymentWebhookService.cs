using System.Text.Json;
using CasamentoAnaKaio.Application.Abstractions;

namespace CasamentoAnaKaio.Application.Services;

public sealed class PaymentWebhookService(
    IGiftContributionRepository contributionRepository,
    IPaymentWebhookValidator webhookValidator,
    IUnitOfWork unitOfWork)
{
    public async Task<bool> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken)
    {
        if (!webhookValidator.ValidateSignature(payload, signature))
        {
            return false;
        }

        var providerPaymentId = TryReadString(payload, "data.id")
            ?? TryReadString(payload, "id")
            ?? TryReadString(payload, "payment_id");

        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            return false;
        }

        var contribution = await contributionRepository.GetByProviderPaymentIdAsync(providerPaymentId, cancellationToken);
        if (contribution is null)
        {
            return false;
        }

        var status = TryReadString(payload, "status")?.ToLowerInvariant();
        if (status is "approved" or "paid")
        {
            contribution.MarkAsPaid();
        }
        else if (status is "rejected" or "cancelled" or "canceled" or "failed")
        {
            contribution.MarkAsFailed();
        }
        else
        {
            return true;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ValidateWebhookSignatureAsync(string payload, string signature)
    {
        return Task.FromResult(webhookValidator.ValidateSignature(payload, signature));
    }

    private static string? TryReadString(string payload, string path)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var current = document.RootElement;

            foreach (var segment in path.Split('.'))
            {
                if (!current.TryGetProperty(segment, out current))
                {
                    return null;
                }
            }

            return current.ValueKind switch
            {
                JsonValueKind.String => current.GetString(),
                JsonValueKind.Number => current.GetRawText(),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
