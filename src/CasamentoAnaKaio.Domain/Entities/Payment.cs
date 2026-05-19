using System;
using System.Collections.Generic;
using System.Text;

namespace CasamentoAnaKaio.Domain.Entities;
public class Payment
{
    public Guid Id { get; set; }

    public Guid GiftContributionId { get; set; }

    public decimal Amount { get; set; }

    public string MercadoPagoPaymentId { get; set; } = string.Empty;

    public string Status { get; set; } = "pending";

    public string PixQrCode { get; set; } = string.Empty;

    public string PixCopyPaste { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
