using System.Text.Json;
using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;

namespace CasamentoAnaKaio.Application.Services;

public sealed class PaymentWebhookService(
    IGiftContributionRepository contributionRepository,
    IPaymentRepository paymentRepository,
    IPaymentWebhookValidator webhookValidator,
    IMercadoPagoPaymentClient mercadoPagoClient,
    IUnitOfWork unitOfWork)
{
    public async Task<bool> ProcessWebhookAsync(
        string payload,
        string signature,
        string requestId,
        IReadOnlyDictionary<string, string?> query,
        CancellationToken cancellationToken)
    {
        var providerPaymentId = ExtractPaymentId(payload, query);
        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            return false;
        }

        if (!webhookValidator.ValidateSignature(providerPaymentId, requestId, signature))
        {
            return false;
        }

        var mercadoPagoPayment = await mercadoPagoClient.GetPaymentAsync(providerPaymentId, cancellationToken);
        var status = PaymentService.MapMercadoPagoStatus(mercadoPagoPayment.Status);
        var payment = await FindPaymentAsync(mercadoPagoPayment, cancellationToken);
        if (payment is null)
        {
            return true;
        }

        payment.SetMercadoPagoPaymentId(mercadoPagoPayment.Id);
        payment.SetPixData(mercadoPagoPayment.QrCodeBase64, mercadoPagoPayment.QrCode);
        payment.SetStatus(status);

        var contribution = await contributionRepository.GetByIdAsync(payment.GiftContributionId, cancellationToken);
        if (contribution is not null)
        {
            contribution.SetProviderPaymentId(mercadoPagoPayment.Id);
            ApplyContributionStatus(contribution, status);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static string? ExtractPaymentId(string payload, IReadOnlyDictionary<string, string?> query)
    {
        return TryReadQuery(query, "data.id")
            ?? TryReadQuery(query, "id")
            ?? TryReadQuery(query, "payment_id")
            ?? TryReadString(payload, "data.id")
            ?? TryReadString(payload, "id")
            ?? TryReadString(payload, "payment_id")
            ?? TryReadString(payload, "resource.id");
    }

    private async Task<Payment?> FindPaymentAsync(
        MercadoPagoPaymentDetails mercadoPagoPayment,
        CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByMercadoPagoPaymentIdAsync(mercadoPagoPayment.Id, cancellationToken);
        if (payment is not null)
        {
            return payment;
        }

        return string.IsNullOrWhiteSpace(mercadoPagoPayment.ExternalReference)
            ? null
            : await paymentRepository.GetByExternalReferenceAsync(mercadoPagoPayment.ExternalReference, cancellationToken);
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
            case PaymentStatus.Failed:
                contribution.MarkAsFailed();
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
        }
    }

    private static string? TryReadQuery(IReadOnlyDictionary<string, string?> query, string key)
    {
        return query.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
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
