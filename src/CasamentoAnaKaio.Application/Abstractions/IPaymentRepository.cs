using CasamentoAnaKaio.Domain.Entities;

namespace CasamentoAnaKaio.Application.Abstractions;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Payment?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken);
    Task<Payment?> GetByMercadoPagoPaymentIdAsync(string mercadoPagoPaymentId, CancellationToken cancellationToken);
}
