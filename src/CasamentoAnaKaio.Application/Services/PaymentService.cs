using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Contracts.Payments;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;

namespace CasamentoAnaKaio.Application.Services;

public sealed class PaymentService(
    IGiftRepository giftRepository,
    IGiftContributionRepository contributionRepository,
    IPaymentRepository paymentRepository,
    IMercadoPagoPaymentClient mercadoPagoClient,
    IUnitOfWork unitOfWork)
{
    public async Task<CreatePaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var gift = await giftRepository.GetByIdAsync(request.GiftId, cancellationToken)
            ?? throw new InvalidOperationException("Presente nao encontrado.");

        if (!Enum.TryParse<GiftContributionMode>(request.Mode, true, out var mode))
        {
            throw new ArgumentException("Modo de contribuicao invalido.", nameof(request.Mode));
        }

        var paymentMethod = NormalizePaymentMethod(request.PaymentMethod);
        var quotaQuantity = mode == GiftContributionMode.FullGift ? 0 : request.QuotaQuantity;
        var amount = GiftContribution.CalculateAmount(gift.Price, mode, quotaQuantity);

        var contribution = new GiftContribution(
            gift,
            request.PayerName,
            request.PayerPhone,
            mode,
            quotaQuantity,
            string.Empty,
            string.Empty,
            string.Empty);

        var externalReference = contribution.Id.ToString("N");
        var payment = new Payment(
            contribution.Id,
            amount,
            paymentMethod,
            request.PayerName,
            request.PayerEmail,
            externalReference);

        var preference = await mercadoPagoClient.CreateCheckoutPreferenceAsync(
            new MercadoPagoPreferenceRequest(
                $"Presente Ana e Kaio - {gift.Name}",
                amount,
                request.PayerName,
                BuildPayerEmail(request.PayerEmail),
                paymentMethod,
                externalReference),
            $"{contribution.Id:N}-{payment.Id:N}-{paymentMethod}",
            cancellationToken);

        EnsureCheckoutPreferenceHasPaymentUrl(preference);
        payment.SetCheckoutPreference(preference.Id, preference.InitPoint, preference.SandboxInitPoint);

        await contributionRepository.AddAsync(contribution, cancellationToken);
        await paymentRepository.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapCreated(payment, contribution);
    }

    public async Task<PaymentStatusResponse?> GetStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByIdAsync(id, cancellationToken);
        if (payment is null)
        {
            return null;
        }

        var contribution = await contributionRepository.GetByIdAsync(payment.GiftContributionId, cancellationToken);
        return new PaymentStatusResponse(
            payment.Id,
            payment.GiftContributionId,
            payment.Status,
            payment.PaymentMethod,
            payment.Amount,
            contribution?.PaidAt);
    }

    public static PaymentStatus MapMercadoPagoStatus(string? mercadoPagoStatus)
    {
        return mercadoPagoStatus?.ToLowerInvariant() switch
        {
            "approved" => PaymentStatus.Paid,
            "pending" => PaymentStatus.Pending,
            "in_process" => PaymentStatus.Processing,
            "rejected" => PaymentStatus.Failed,
            "cancelled" => PaymentStatus.Cancelled,
            "canceled" => PaymentStatus.Cancelled,
            "refunded" => PaymentStatus.Refunded,
            "charged_back" => PaymentStatus.ChargedBack,
            "expired" => PaymentStatus.Expired,
            _ => PaymentStatus.Unknown
        };
    }

    private static CreatePaymentResponse MapCreated(Payment payment, GiftContribution contribution)
    {
        var checkoutUrl = string.IsNullOrWhiteSpace(payment.SandboxInitPoint)
            ? payment.InitPoint
            : payment.SandboxInitPoint;

        return new CreatePaymentResponse(
            payment.Id,
            contribution.Id,
            payment.Status,
            payment.PaymentMethod,
            payment.Amount,
            checkoutUrl,
            payment.InitPoint,
            payment.SandboxInitPoint,
            checkoutUrl,
            null,
            null,
            null,
            string.IsNullOrWhiteSpace(payment.PixCopyPaste) ? null : payment.PixCopyPaste,
            string.IsNullOrWhiteSpace(payment.PixQrCode) ? null : payment.PixQrCode,
            string.IsNullOrWhiteSpace(payment.PixCopyPaste) ? null : payment.PixCopyPaste,
            payment.PreferenceId,
            string.IsNullOrWhiteSpace(payment.MercadoPagoPaymentId) ? null : payment.MercadoPagoPaymentId,
            payment.ExternalReference);
    }

    private static void EnsureCheckoutPreferenceHasPaymentUrl(MercadoPagoPreferenceResult preference)
    {
        if (string.IsNullOrWhiteSpace(preference.InitPoint) &&
            string.IsNullOrWhiteSpace(preference.SandboxInitPoint))
        {
            throw new InvalidOperationException("Mercado Pago criou a preferencia, mas nao retornou link de pagamento.");
        }
    }

    private static string NormalizePaymentMethod(string paymentMethod)
    {
        return paymentMethod.Trim().ToLowerInvariant() switch
        {
            "pix" => "pix",
            "boleto" => "boleto",
            "credit_card" => "credit_card",
            "credit-card" => "credit_card",
            _ => throw new ArgumentException("Forma de pagamento invalida.", nameof(paymentMethod))
        };
    }

    private static string BuildPayerEmail(string? payerEmail)
    {
        return string.IsNullOrWhiteSpace(payerEmail)
            ? "convidado+pagamento@casamento-ana-kaio.local"
            : payerEmail.Trim();
    }
}
