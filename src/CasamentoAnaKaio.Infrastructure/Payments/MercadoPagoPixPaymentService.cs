using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Infrastructure.Options;
using MercadoPago.Client.Payment;
using Serilog;

namespace CasamentoAnaKaio.Infrastructure.Payments;

public sealed class MercadoPagoPixPaymentService(
    MercadoPagoOptions options) : IPixPaymentService
{
    public PixPayment CreatePayment(string description, decimal amount)
    {
        try
        {
            var client = new PaymentClient();

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = (decimal?)amount,
                Description = description,
                PaymentMethodId = "pix",
                Payer = new PaymentPayerRequest
                {
                    Email = "guest@casamento.com"
                },
                Metadata = new Dictionary<string, object>
                {
                    { "origin", "web" },
                    { "payment_type", "pix" }
                }
            };

            var payment = client.Create(paymentRequest);

            if (payment?.Id is null)
                throw new InvalidOperationException("Falha ao gerar pagamento PIX no Mercado Pago.");

            // Extrair informações PIX da resposta
            var pixQrCode = payment.PointOfInteraction?.TransactionData?.QrCode ?? string.Empty;
            var pixCopyPaste = pixQrCode; // QR Code é usado para copy/paste também

            Log.Information(
                "Pagamento PIX criado com sucesso: PaymentId={PaymentId}, Amount={Amount}, Description={Description}",
                payment.Id, amount, description);

            return new PixPayment(
                PixKey: options.ExternalPosId,
                QrCodePayload: pixQrCode,
                QrCodeUrl: pixCopyPaste,
                ProviderPaymentId: payment.Id.ToString() ?? string.Empty);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao criar pagamento PIX: Amount={Amount}, Description={Description}",
                amount, description);
            throw;
        }
    }
}
