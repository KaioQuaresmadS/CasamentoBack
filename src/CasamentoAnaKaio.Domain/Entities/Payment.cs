using CasamentoAnaKaio.Domain.Enums;

namespace CasamentoAnaKaio.Domain.Entities;

public class Payment
{
    private Payment()
    {
    }

    public Payment(
        Guid giftContributionId,
        decimal amount,
        string paymentMethod,
        string payerName,
        string? payerEmail,
        string externalReference)
    {
        GiftContributionId = giftContributionId;
        Amount = amount;
        PaymentMethod = paymentMethod.Trim();
        PayerName = payerName.Trim();
        PayerEmail = payerEmail?.Trim() ?? string.Empty;
        ExternalReference = externalReference.Trim();
        Status = PaymentStatus.Pending.ToString();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid GiftContributionId { get; private set; }
    public decimal Amount { get; private set; }
    public string MercadoPagoPaymentId { get; private set; } = string.Empty;
    public string PreferenceId { get; private set; } = string.Empty;
    public string InitPoint { get; private set; } = string.Empty;
    public string SandboxInitPoint { get; private set; } = string.Empty;
    public string ExternalReference { get; private set; } = string.Empty;
    public string PaymentMethod { get; private set; } = string.Empty;
    public string PayerName { get; private set; } = string.Empty;
    public string PayerEmail { get; private set; } = string.Empty;
    public string Status { get; private set; } = PaymentStatus.Pending.ToString();
    public string PixQrCode { get; private set; } = string.Empty;
    public string PixCopyPaste { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

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

    public void SetPixData(string? qrCode, string? copyPaste)
    {
        PixQrCode = qrCode ?? string.Empty;
        PixCopyPaste = copyPaste ?? string.Empty;
        Touch();
    }

    public void SetStatus(PaymentStatus status)
    {
        Status = status.ToString();
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
