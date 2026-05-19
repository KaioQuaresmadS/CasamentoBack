using MercadoPago.Client.Payment;
using MercadoPago.Resource.Payment;

namespace CasamentoAnaKaio.Infrastructure.Payments;

public class MercadoPagoService
{
    public async Task<Payment> CreatePixPaymentAsync(
        decimal amount,
        string description,
        string email)
    {
        var client = new PaymentClient();

        var request = new PaymentCreateRequest
        {
            TransactionAmount = amount,

            Description = description,

            PaymentMethodId = "pix",

            Payer = new PaymentPayerRequest
            {
                Email = email
            }
        };

        return await client.CreateAsync(request);
    }
}