namespace CasamentoAnaKaio.Application.Abstractions;

public interface IPixPaymentService
{
    PixPayment CreatePayment(string description, decimal amount);
}

public sealed record PixPayment(
    string PixKey,
    string QrCodePayload,
    string QrCodeUrl,
    string ProviderPaymentId);
