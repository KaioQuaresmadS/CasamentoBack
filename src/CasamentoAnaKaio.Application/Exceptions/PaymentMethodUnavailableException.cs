namespace CasamentoAnaKaio.Application.Exceptions;

public sealed class PaymentMethodUnavailableException(string message) : Exception(message);
