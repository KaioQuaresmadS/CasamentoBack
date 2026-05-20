using System.Text.Json;
using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CasamentoAnaKaio.Application.Services;

public sealed class PaymentWebhookService(
    IGiftContributionRepository contributionRepository,
    IPaymentRepository paymentRepository,
    IMercadoPagoPaymentClient mercadoPagoPaymentClient,
    IPaymentWebhookValidator webhookValidator,
    IUnitOfWork unitOfWork,
    ILogger<PaymentWebhookService> logger)
{
    public async Task<bool> ProcessWebhookAsync(
        string payload,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyDictionary<string, string> query,
        CancellationToken cancellationToken)
    {
        var paymentId = ExtractPaymentId(payload, query);
        var signature = GetHeader(headers, "x-signature");
        var requestId = GetHeader(headers, "x-request-id");

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            logger.LogWarning("Mercado Pago webhook ignored because payment id was not found. Headers={Headers} Payload={Payload}", headers, payload);
            return false;
        }

        if (!webhookValidator.ValidateSignature(paymentId, requestId, signature))
        {
            logger.LogWarning("Mercado Pago webhook rejected. PaymentId={PaymentId} RequestId={RequestId}", paymentId, requestId);
            return false;
        }

        var details = await mercadoPagoPaymentClient.GetPaymentAsync(paymentId, cancellationToken);
        var internalStatus = PaymentService.MapStatus(details.Status);

        var payment = await paymentRepository.GetByMercadoPagoPaymentIdAsync(details.PaymentId, cancellationToken)
            ?? await paymentRepository.GetByExternalReferenceAsync(details.ExternalReference, cancellationToken);

        if (payment is null)
        {
            logger.LogWarning(
                "Mercado Pago webhook payment not found locally. PaymentId={PaymentId} ExternalReference={ExternalReference} Status={Status}",
                details.PaymentId,
                details.ExternalReference,
                details.Status);
            return false;
        }

        var oldStatus = payment.Status;
        payment.SetMercadoPagoPaymentId(details.PaymentId);
        payment.SetStatus(internalStatus, details.Status);
        if (!string.IsNullOrWhiteSpace(details.PixQrCode) || !string.IsNullOrWhiteSpace(details.PixCopyPaste))
        {
            payment.SetPixData(details.PixQrCode ?? string.Empty, details.PixCopyPaste ?? string.Empty);
        }

        var contribution = await contributionRepository.GetByIdAsync(payment.GiftContributionId, cancellationToken);
        if (contribution is not null)
        {
            contribution.SetProviderPaymentId(details.PaymentId);
            ApplyContributionStatus(contribution, internalStatus);
        }

        logger.LogInformation(
            "Mercado Pago webhook processed. PaymentId={PaymentId} ExternalReference={ExternalReference} OldStatus={OldStatus} NewStatus={NewStatus} MercadoPagoStatus={MercadoPagoStatus}",
            details.PaymentId,
            details.ExternalReference,
            oldStatus,
            payment.Status,
            details.Status);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ValidateWebhookSignatureAsync(
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyDictionary<string, string> query)
    {
        var paymentId = query.TryGetValue("data.id", out var queryPaymentId) ? queryPaymentId : string.Empty;
        return Task.FromResult(webhookValidator.ValidateSignature(
            paymentId,
            GetHeader(headers, "x-request-id"),
            GetHeader(headers, "x-signature")));
    }

    private static void ApplyContributionStatus(GiftContribution contribution, PaymentStatus status)
    {
        switch (status)
        {
            case PaymentStatus.Paid:
                contribution.MarkAsPaid();
                break;
            case PaymentStatus.Pending:
                contribution.MarkAsPending();
                break;
            case PaymentStatus.Processing:
                contribution.MarkAsProcessing();
                break;
            case PaymentStatus.Cancelled:
                contribution.MarkAsCancelled();
                break;
            case PaymentStatus.Refunded:
                contribution.MarkAsRefunded();
                break;
            case PaymentStatus.ChargedBack:
                contribution.MarkAsChargedBack();
                break;
            case PaymentStatus.Expired:
                contribution.MarkAsExpired();
                break;
            case PaymentStatus.Failed:
                contribution.MarkAsFailed();
                break;
            default:
                contribution.MarkAsUnknown();
                break;
        }
    }

    private static string ExtractPaymentId(string payload, IReadOnlyDictionary<string, string> query)
    {
        if (query.TryGetValue("data.id", out var dataIdFromQuery) && !string.IsNullOrWhiteSpace(dataIdFromQuery))
        {
            return dataIdFromQuery;
        }

        return TryReadString(payload, "data.id")
            ?? TryReadString(payload, "id")
            ?? TryReadString(payload, "payment_id")
            ?? string.Empty;
    }

    private static string GetHeader(IReadOnlyDictionary<string, string> headers, string name)
    {
        return headers.TryGetValue(name, out var value) ? value : string.Empty;
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
