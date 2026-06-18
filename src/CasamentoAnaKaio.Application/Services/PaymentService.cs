using System.Net.Mail;
using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Contracts.Payments;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;
using Serilog;

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

    public async Task<CreatePixPaymentResponse> CreatePixAsync(
        CreatePixPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ValidatePixRequest(request);

        var gift = await giftRepository.GetByIdAsync(request.GiftId, cancellationToken)
            ?? throw new ArgumentException("Presente nao encontrado.", nameof(request.GiftId));

        var payerName = request.PayerName.Trim();
        var payerEmail = request.PayerEmail.Trim();
        var externalReference = $"pix-{Guid.NewGuid():N}";
        var description = $"Presente Ana e Kaio - {gift.Name}";

        var contribution = new GiftContribution(
            gift,
            payerName,
            "nao-informado",
            GiftContributionMode.FullGift,
            0,
            "mercado-pago-pix",
            string.Empty,
            string.Empty);

        var payment = new Payment(
            contribution.Id,
            request.Amount,
            "Pix",
            payerName,
            payerEmail,
            externalReference);

        Log.Information(
            "Criando pagamento Pix Mercado Pago. GiftId={GiftId}, Amount={Amount}, ExternalReference={ExternalReference}",
            request.GiftId,
            request.Amount,
            externalReference);

        MercadoPagoPaymentDetails mercadoPagoPayment;
        try
        {
            mercadoPagoPayment = await mercadoPagoClient.CreatePixPaymentAsync(
                new MercadoPagoPixPaymentRequest(
                    description,
                    request.Amount,
                    payerName,
                    payerEmail,
                    externalReference),
                Guid.NewGuid().ToString("N"),
                cancellationToken);
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Erro na API Mercado Pago ao criar Pix. GiftId={GiftId}, Amount={Amount}, ExternalReference={ExternalReference}",
                request.GiftId,
                request.Amount,
                externalReference);
            throw;
        }

        var status = MapMercadoPagoStatus(mercadoPagoPayment.Status);
        payment.SetMercadoPagoPaymentId(mercadoPagoPayment.Id);
        payment.SetPixData(
            mercadoPagoPayment.QrCode,
            mercadoPagoPayment.QrCodeBase64,
            mercadoPagoPayment.TicketUrl);
        payment.SetStatus(status);
        contribution.SetProviderPaymentId(mercadoPagoPayment.Id);

        await contributionRepository.AddAsync(contribution, cancellationToken);
        await paymentRepository.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        Log.Information(
            "Pagamento Pix Mercado Pago criado. PaymentId={PaymentId}, Status={Status}, ExternalReference={ExternalReference}",
            mercadoPagoPayment.Id,
            mercadoPagoPayment.Status,
            externalReference);

        return new CreatePixPaymentResponse(
            mercadoPagoPayment.Id,
            NormalizeMercadoPagoStatusForResponse(mercadoPagoPayment.Status),
            mercadoPagoPayment.QrCode ?? string.Empty,
            mercadoPagoPayment.QrCodeBase64 ?? string.Empty,
            mercadoPagoPayment.TicketUrl ?? string.Empty,
            externalReference);
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
            string.IsNullOrWhiteSpace(payment.QrCodeBase64) ? null : payment.QrCodeBase64,
            string.IsNullOrWhiteSpace(payment.PixCopyPaste) ? null : payment.PixCopyPaste,
            payment.PreferenceId,
            string.IsNullOrWhiteSpace(payment.MercadoPagoPaymentId) ? null : payment.MercadoPagoPaymentId,
            payment.ExternalReference);
    }

    private static void ValidatePixRequest(CreatePixPaymentRequest request)
    {
        if (request.GiftId == Guid.Empty)
        {
            throw new ArgumentException("giftId e obrigatorio.", nameof(request.GiftId));
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "amount deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(request.PayerEmail))
        {
            throw new ArgumentException("payerEmail e obrigatorio.", nameof(request.PayerEmail));
        }

        try
        {
            _ = new MailAddress(request.PayerEmail.Trim());
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("payerEmail invalido.", nameof(request.PayerEmail), exception);
        }
    }

    private static string NormalizeMercadoPagoStatusForResponse(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "pending"
            : status.Trim().ToLowerInvariant();
    }

    private static void EnsureCheckoutPreferenceHasPaymentUrl(MercadoPagoPreferenceResult preference)
    {
        if (string.IsNullOrWhiteSpace(preference.InitPoint) &&
            string.IsNullOrWhiteSpace(preference.SandboxInitPoint))
        {
            throw new InvalidOperationException("Mercado Pago criou a preferencia, mas nao retornou link de pagamento.");
        }
    }

    private static string NormalizePaymentMethod(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return "mercado_pago";
        }

        return paymentMethod.Trim().ToLowerInvariant() switch
        {
            "pix" => "pix",
            "boleto" => "boleto",
            "mercado_pago" => "mercado_pago",
            "mercado-pago" => "mercado_pago",
            "checkout_pro" => "mercado_pago",
            "checkout-pro" => "mercado_pago",
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
