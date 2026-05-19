namespace CasamentoAnaKaio.Application.Abstractions;

public interface IPaymentWebhookService
{
    Task<bool> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken);
    Task<bool> ValidateWebhookSignatureAsync(string payload, string signature);
}
