using CasamentoAnaKaio.Domain.Enums;

namespace CasamentoAnaKaio.Domain.Entities;

public class Payment
{
    private Payment()
    {
    }

    public Payment(
        Guid giftContributionId,
        string externalReference,
        string paymentMethod,
        decimal amount,
        string payerName,
        string payerEmail)
    {
        GiftContributionId = giftContributionId;
        ExternalReference = externalReference;
        PaymentMethod = paymentMethod;
        Amount = amount;
        PayerName = payerName.Trim();
        PayerEmail = payerEmail.Trim();
        Status = PaymentStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid GiftContributionId { get; private set; }
    public string MercadoPagoPaymentId { get; private set; } = string.Empty;
    public string PreferenceId { get; private set; } = string.Empty;
    public string InitPoint { get; private set; } = string.Empty;
    public string SandboxInitPoint { get; private set; } = string.Empty;
    public string ExternalReference { get; private set; } = string.Empty;
    public string PaymentMethod { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string PayerName { get; private set; } = string.Empty;
    public string PayerEmail { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public string MercadoPagoStatus { get; private set; } = "pending";
    public string PixQrCode { get; private set; } = string.Empty;
    public string PixCopyPaste { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }

    public void SetCheckoutPreference(string preferenceId, string initPoint, string sandboxInitPoint)
    {
        PreferenceId = preferenceId;
        InitPoint = initPoint;
        SandboxInitPoint = sandboxInitPoint;
        Touch();
    }

    public void SetMercadoPagoPaymentId(string mercadoPagoPaymentId)
    {
        if (!string.IsNullOrWhiteSpace(mercadoPagoPaymentId))
        {
            MercadoPagoPaymentId = mercadoPagoPaymentId.Trim();
            Touch();
        }
    }

    public void SetPixData(string qrCode, string copyPaste)
    {
        PixQrCode = qrCode;
        PixCopyPaste = copyPaste;
        Touch();
    }

    public void SetStatus(PaymentStatus status, string mercadoPagoStatus)
    {
        if (Status == status && MercadoPagoStatus == mercadoPagoStatus)
        {
            return;
        }

        Status = status;
        MercadoPagoStatus = mercadoPagoStatus;
        if (status == PaymentStatus.Paid && PaidAt is null)
        {
            PaidAt = DateTimeOffset.UtcNow;
        }

        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
