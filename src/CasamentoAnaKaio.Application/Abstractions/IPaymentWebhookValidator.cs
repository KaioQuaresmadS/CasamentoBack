namespace CasamentoAnaKaio.Application.Abstractions;

public interface IPaymentWebhookValidator
{
    bool ValidateSignature(string dataId, string xRequestId, string xSignature);
}
