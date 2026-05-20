using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Contracts.Payments;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CasamentoAnaKaio.Application.Services;

public sealed class PaymentService(
    IGiftRepository giftRepository,
    IGiftContributionRepository contributionRepository,
    IPaymentRepository paymentRepository,
    IMercadoPagoPaymentClient mercadoPagoPaymentClient,
    IUnitOfWork unitOfWork,
    ILogger<PaymentService> logger)
{
    public async Task<CreatePaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        string frontendUrl,
        string backendUrl,
        CancellationToken cancellationToken)
    {
        var gift = await giftRepository.GetByIdAsync(request.GiftId, cancellationToken)
            ?? throw new InvalidOperationException("Presente nao encontrado.");

        if (!Enum.TryParse<GiftContributionMode>(request.Mode, true, out var mode))
        {
            throw new ArgumentException("Modo de contribuicao invalido.", nameof(request.Mode));
        }

        var paymentMethod = NormalizePaymentMethod(request.PaymentMethod);
        var payerName = EnsureRequired(request.PayerName, "Nome do pagador e obrigatorio.");
        var payerEmail = EnsureRequired(request.PayerEmail, "Email do pagador e obrigatorio.");
        var payerPhone = string.IsNullOrWhiteSpace(request.PayerPhone) ? "nao informado" : request.PayerPhone.Trim();

        var contribution = new GiftContribution(
            gift,
            payerName,
            payerPhone,
            mode,
            request.QuotaQuantity,
            "mercado-pago",
            string.Empty,
            $"pending-{Guid.NewGuid():N}");

        var externalReference = contribution.Id.ToString("N");
        var amount = contribution.Amount;
        var payment = new Payment(
            contribution.Id,
            externalReference,
            paymentMethod,
            amount,
            payerName,
            payerEmail);

        var preferenceRequest = new MercadoPagoPreferenceRequest(
            externalReference,
            $"Presente Ana e Kaio - {gift.Name}",
            amount,
            payerName,
            payerEmail,
            paymentMethod,
            BuildReturnUrl(frontendUrl, "success", payment.Id),
            BuildReturnUrl(frontendUrl, "failure", payment.Id),
            BuildReturnUrl(frontendUrl, "pending", payment.Id),
            $"{backendUrl.TrimEnd('/')}/api/payments/webhook/mercadopago");

        var idempotencyKey = $"gift-{request.GiftId:N}-{externalReference}-{paymentMethod}";
        var preference = await mercadoPagoPaymentClient.CreatePreferenceAsync(
            preferenceRequest,
            idempotencyKey,
            cancellationToken);

        payment.SetCheckoutPreference(preference.PreferenceId, preference.InitPoint, preference.SandboxInitPoint);
        contribution.SetProviderPaymentId(preference.PreferenceId);

        await contributionRepository.AddAsync(contribution, cancellationToken);
        await paymentRepository.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Mercado Pago preference created. PaymentId={PaymentId} PreferenceId={PreferenceId} ExternalReference={ExternalReference} Method={PaymentMethod} Amount={Amount}",
            payment.Id,
            preference.PreferenceId,
            externalReference,
            paymentMethod,
            amount);

        return new CreatePaymentResponse(
            payment.Id,
            contribution.Id,
            payment.Status.ToString(),
            payment.PaymentMethod,
            payment.Amount,
            payment.PreferenceId,
            payment.InitPoint,
            payment.SandboxInitPoint,
            payment.CreatedAt);
    }

    public async Task<PaymentStatusResponse?> GetStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByIdAsync(id, cancellationToken);
        if (payment is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(payment.MercadoPagoPaymentId))
        {
            var details = await mercadoPagoPaymentClient.GetPaymentAsync(payment.MercadoPagoPaymentId, cancellationToken);
            var internalStatus = MapStatus(details.Status);
            payment.SetStatus(internalStatus, details.Status);
            if (!string.IsNullOrWhiteSpace(details.PixQrCode) || !string.IsNullOrWhiteSpace(details.PixCopyPaste))
            {
                payment.SetPixData(details.PixQrCode ?? string.Empty, details.PixCopyPaste ?? string.Empty);
            }

            var contribution = await contributionRepository.GetByIdAsync(payment.GiftContributionId, cancellationToken);
            if (contribution is not null)
            {
                ApplyContributionStatus(contribution, internalStatus);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new PaymentStatusResponse(
            payment.Id,
            payment.GiftContributionId,
            payment.Status.ToString(),
            payment.MercadoPagoStatus,
            payment.PaidAt,
            payment.UpdatedAt);
    }

    public static PaymentStatus MapStatus(string? mercadoPagoStatus)
    {
        return mercadoPagoStatus?.Trim().ToLowerInvariant() switch
        {
            "approved" => PaymentStatus.Paid,
            "pending" => PaymentStatus.Pending,
            "in_process" => PaymentStatus.Processing,
            "rejected" => PaymentStatus.Failed,
            "cancelled" or "canceled" => PaymentStatus.Cancelled,
            "refunded" => PaymentStatus.Refunded,
            "charged_back" => PaymentStatus.ChargedBack,
            "expired" => PaymentStatus.Expired,
            _ => PaymentStatus.Unknown
        };
    }

    private static string NormalizePaymentMethod(string paymentMethod)
    {
        return paymentMethod.Trim().ToLowerInvariant().Replace("-", "_") switch
        {
            "pix" => "pix",
            "boleto" => "boleto",
            "ticket" => "boleto",
            "credit_card" => "credit_card",
            "card" => "credit_card",
            _ => throw new ArgumentException("Forma de pagamento invalida.", nameof(paymentMethod))
        };
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

    private static string EnsureRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message);
        }

        return value.Trim();
    }

    private static string BuildReturnUrl(string frontendUrl, string status, Guid paymentId)
    {
        return $"{frontendUrl.TrimEnd('/')}/?paymentId={paymentId:N}&paymentStatus={status}";
    }
}
