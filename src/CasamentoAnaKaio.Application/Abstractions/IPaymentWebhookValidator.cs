namespace CasamentoAnaKaio.Application.Abstractions;

public interface IPaymentWebhookValidator
{
    bool ValidateSignature(string payload, string signature);
}
