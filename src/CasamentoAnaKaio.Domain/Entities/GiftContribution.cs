using CasamentoAnaKaio.Domain.Enums;

namespace CasamentoAnaKaio.Domain.Entities;

public class GiftContribution
{
    private GiftContribution()
    {
    }

    public GiftContribution(
        Gift gift,
        string contributorName,
        string contributorPhone,
        GiftContributionMode mode,
        int quotaQuantity,
        string pixKey,
        string qrCodePayload,
        string providerPaymentId)
    {
        if (string.IsNullOrWhiteSpace(contributorName))
        {
            throw new ArgumentException("Nome do contribuinte e obrigatorio.", nameof(contributorName));
        }

        if (string.IsNullOrWhiteSpace(contributorPhone))
        {
            throw new ArgumentException("Celular do contribuinte e obrigatorio.", nameof(contributorPhone));
        }

        if (mode == GiftContributionMode.Quota && quotaQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quotaQuantity), "Quantidade de cotas deve ser maior que zero.");
        }

        GiftId = gift.Id;
        Gift = gift;
        ContributorName = contributorName.Trim();
        ContributorPhone = contributorPhone.Trim();
        Mode = mode;
        QuotaQuantity = mode == GiftContributionMode.FullGift ? 0 : quotaQuantity;
        Amount = CalculateAmount(gift.Price, mode, QuotaQuantity);
        PaymentStatus = PaymentStatus.Pending;
        PixKey = pixKey;
        QrCodePayload = qrCodePayload;
        ProviderPaymentId = providerPaymentId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid GiftId { get; private set; }
    public Gift? Gift { get; private set; }
    public string ContributorName { get; private set; } = string.Empty;
    public string ContributorPhone { get; private set; } = string.Empty;
    public GiftContributionMode Mode { get; private set; }
    public int QuotaQuantity { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public string PixKey { get; private set; } = string.Empty;
    public string QrCodePayload { get; private set; } = string.Empty;
    public string ProviderPaymentId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }

    public static decimal CalculateAmount(decimal giftPrice, GiftContributionMode mode, int quotaQuantity)
    {
        return mode == GiftContributionMode.FullGift ? giftPrice : giftPrice * 0.05m * quotaQuantity;
    }

    public void MarkAsPaid()
    {
        if (PaymentStatus == PaymentStatus.Paid)
        {
            return;
        }

        PaymentStatus = PaymentStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsFailed()
    {
        PaymentStatus = PaymentStatus.Failed;
    }

    public void MarkAsPending()
    {
        PaymentStatus = PaymentStatus.Pending;
    }

    public void MarkAsProcessing()
    {
        PaymentStatus = PaymentStatus.Processing;
    }

    public void MarkAsCancelled()
    {
        PaymentStatus = PaymentStatus.Cancelled;
    }

    public void MarkAsRefunded()
    {
        PaymentStatus = PaymentStatus.Refunded;
    }

    public void MarkAsChargedBack()
    {
        PaymentStatus = PaymentStatus.ChargedBack;
    }

    public void MarkAsExpired()
    {
        PaymentStatus = PaymentStatus.Expired;
    }

    public void MarkAsUnknown()
    {
        PaymentStatus = PaymentStatus.Unknown;
    }

    public void SetProviderPaymentId(string providerPaymentId)
    {
        if (!string.IsNullOrWhiteSpace(providerPaymentId))
        {
            ProviderPaymentId = providerPaymentId.Trim();
        }
    }
}
