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
        if (paymentMethod == "credit_card")
        {
            throw new InvalidOperationException("Pagamento por cartao esta temporariamente desativado. Use Pix ou boleto para concluir o presente sem abrir o aplicativo do Mercado Pago.");
        }

        if (paymentMethod == "boleto")
        {
            var boleto = await CreateBoletoCoreAsync(gift, request, mode, cancellationToken);
            return MapCreated(boleto.Payment, boleto.Contribution);
        }

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

    public async Task<CreateBoletoPaymentResponse> CreateBoletoAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var gift = await giftRepository.GetByIdAsync(request.GiftId, cancellationToken)
            ?? throw new InvalidOperationException("Presente nao encontrado.");

        if (!Enum.TryParse<GiftContributionMode>(request.Mode, true, out var mode))
        {
            throw new ArgumentException("Modo de contribuicao invalido.", nameof(request.Mode));
        }

        var boleto = await CreateBoletoCoreAsync(gift, request, mode, cancellationToken);

        return new CreateBoletoPaymentResponse(
            boleto.Payment.Id,
            boleto.Contribution.Id,
            boleto.MercadoPagoPayment?.Id ?? string.Empty,
            NormalizeMercadoPagoStatusForResponse(boleto.MercadoPagoPayment?.Status ?? boleto.Payment.Status),
            EmptyToNull(boleto.Payment.TicketUrl),
            EmptyToNull(boleto.Payment.TicketUrl),
            EmptyToNull(boleto.Payment.Barcode),
            EmptyToNull(boleto.Payment.LinhaDigitavel),
            boleto.Payment.ExternalReference,
            BuildCheckoutUrl(boleto.Payment),
            BuildPaymentUrl(boleto.Payment));
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

    public async Task<PaymentStatusResponse?> GetMercadoPagoStatusAsync(
        string mercadoPagoPaymentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mercadoPagoPaymentId))
        {
            return null;
        }

        var mercadoPagoPayment = await mercadoPagoClient.GetPaymentAsync(mercadoPagoPaymentId.Trim(), cancellationToken);
        var payment = await paymentRepository.GetByMercadoPagoPaymentIdAsync(mercadoPagoPayment.Id, cancellationToken);
        if (payment is null && !string.IsNullOrWhiteSpace(mercadoPagoPayment.ExternalReference))
        {
            payment = await paymentRepository.GetByExternalReferenceAsync(mercadoPagoPayment.ExternalReference, cancellationToken);
        }

        if (payment is null)
        {
            return null;
        }

        var status = MapMercadoPagoStatus(mercadoPagoPayment.Status);
        payment.SetMercadoPagoPaymentId(mercadoPagoPayment.Id);
        if (HasPixData(mercadoPagoPayment))
        {
            payment.SetPixData(
                mercadoPagoPayment.QrCode,
                mercadoPagoPayment.QrCodeBase64,
                mercadoPagoPayment.TicketUrl);
        }

        if (HasBoletoData(mercadoPagoPayment))
        {
            payment.SetBoletoData(
                mercadoPagoPayment.TicketUrl,
                mercadoPagoPayment.Barcode,
                mercadoPagoPayment.LinhaDigitavel);
        }

        payment.SetStatus(status);

        var contribution = await contributionRepository.GetByIdAsync(payment.GiftContributionId, cancellationToken);
        if (contribution is not null)
        {
            contribution.SetProviderPaymentId(mercadoPagoPayment.Id);
            ApplyContributionStatus(contribution, status);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PaymentStatusResponse(
            payment.Id,
            payment.GiftContributionId,
            payment.Status,
            payment.PaymentMethod,
            payment.Amount,
            contribution?.PaidAt);
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
        var checkoutUrl = BuildCheckoutUrl(payment);

        return new CreatePaymentResponse(
            payment.Id,
            contribution.Id,
            payment.Status,
            payment.PaymentMethod,
            payment.Amount,
            checkoutUrl,
            payment.InitPoint,
            payment.SandboxInitPoint,
            BuildPaymentUrl(payment),
            EmptyToNull(payment.TicketUrl),
            EmptyToNull(payment.TicketUrl),
            EmptyToNull(payment.Barcode),
            EmptyToNull(payment.LinhaDigitavel),
            string.IsNullOrWhiteSpace(payment.PixCopyPaste) ? null : payment.PixCopyPaste,
            string.IsNullOrWhiteSpace(payment.QrCodeBase64) ? null : payment.QrCodeBase64,
            string.IsNullOrWhiteSpace(payment.PixCopyPaste) ? null : payment.PixCopyPaste,
            payment.PreferenceId,
            string.IsNullOrWhiteSpace(payment.MercadoPagoPaymentId) ? null : payment.MercadoPagoPaymentId,
            payment.ExternalReference);
    }

    private static string BuildCheckoutUrl(Payment payment)
    {
        var checkoutUrl = string.IsNullOrWhiteSpace(payment.SandboxInitPoint)
            ? payment.InitPoint
            : payment.SandboxInitPoint;

        return string.IsNullOrWhiteSpace(checkoutUrl)
            ? payment.TicketUrl
            : checkoutUrl;
    }

    private static string? BuildPaymentUrl(Payment payment)
    {
        if (!string.IsNullOrWhiteSpace(payment.TicketUrl))
        {
            return payment.TicketUrl;
        }

        var checkoutUrl = BuildCheckoutUrl(payment);
        return string.IsNullOrWhiteSpace(checkoutUrl) ? null : checkoutUrl;
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

    private async Task<CreatedBoletoPayment> CreateBoletoCoreAsync(
        Gift gift,
        CreatePaymentRequest request,
        GiftContributionMode mode,
        CancellationToken cancellationToken)
    {
        var quotaQuantity = mode == GiftContributionMode.FullGift ? 0 : request.QuotaQuantity;
        var amount = GiftContribution.CalculateAmount(gift.Price, mode, quotaQuantity);
        var payerName = request.PayerName.Trim();
        var payerEmail = BuildPayerEmail(request.PayerEmail);

        var contribution = new GiftContribution(
            gift,
            payerName,
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
            "boleto",
            payerName,
            payerEmail,
            externalReference);

        MercadoPagoPaymentDetails? mercadoPagoPayment = null;

        try
        {
            mercadoPagoPayment = await mercadoPagoClient.CreateBoletoPaymentAsync(
                new MercadoPagoBoletoPaymentRequest(
                    $"Presente Ana e Kaio - {gift.Name}",
                    amount,
                    payerName,
                    payerEmail,
                    externalReference),
                $"{contribution.Id:N}-{payment.Id:N}-boleto",
                cancellationToken);

            EnsureBoletoPaymentHasPaymentData(mercadoPagoPayment);

            var status = MapMercadoPagoStatus(mercadoPagoPayment.Status);
            payment.SetMercadoPagoPaymentId(mercadoPagoPayment.Id);
            payment.SetBoletoData(
                mercadoPagoPayment.TicketUrl,
                mercadoPagoPayment.Barcode,
                mercadoPagoPayment.LinhaDigitavel);
            payment.SetStatus(status);
            contribution.SetProviderPaymentId(mercadoPagoPayment.Id);
        }
        catch (InvalidOperationException exception)
        {
            Log.Warning(
                exception,
                "Boleto direto Mercado Pago indisponivel; criando checkout restrito a boleto. GiftId={GiftId}, ExternalReference={ExternalReference}",
                gift.Id,
                externalReference);

            var preference = await mercadoPagoClient.CreateCheckoutPreferenceAsync(
                new MercadoPagoPreferenceRequest(
                    $"Presente Ana e Kaio - {gift.Name}",
                    amount,
                    payerName,
                    payerEmail,
                    "boleto",
                    externalReference),
                $"{contribution.Id:N}-{payment.Id:N}-boleto-checkout",
                cancellationToken);

            EnsureCheckoutPreferenceHasPaymentUrl(preference);
            payment.SetCheckoutPreference(preference.Id, preference.InitPoint, preference.SandboxInitPoint);
        }

        await contributionRepository.AddAsync(contribution, cancellationToken);
        await paymentRepository.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatedBoletoPayment(contribution, payment, mercadoPagoPayment);
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

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static void EnsureBoletoPaymentHasPaymentData(MercadoPagoPaymentDetails payment)
    {
        if (!HasBoletoData(payment))
        {
            throw new InvalidOperationException("Mercado Pago criou o boleto, mas nao retornou link, codigo de barras ou linha digitavel.");
        }
    }

    private static bool HasPixData(MercadoPagoPaymentDetails payment)
    {
        return !string.IsNullOrWhiteSpace(payment.QrCode) ||
            !string.IsNullOrWhiteSpace(payment.QrCodeBase64);
    }

    private static bool HasBoletoData(MercadoPagoPaymentDetails payment)
    {
        return !string.IsNullOrWhiteSpace(payment.TicketUrl) ||
            !string.IsNullOrWhiteSpace(payment.Barcode) ||
            !string.IsNullOrWhiteSpace(payment.LinhaDigitavel);
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

    private sealed record CreatedBoletoPayment(
        GiftContribution Contribution,
        Payment Payment,
        MercadoPagoPaymentDetails? MercadoPagoPayment);
}
