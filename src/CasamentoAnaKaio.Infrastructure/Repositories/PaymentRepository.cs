using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CasamentoAnaKaio.Infrastructure.Repositories;

public sealed class PaymentRepository(AppDbContext dbContext) : IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        await dbContext.Payments.AddAsync(payment, cancellationToken);
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Payments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken)
    {
        return await dbContext.Payments.FirstOrDefaultAsync(x => x.ExternalReference == externalReference, cancellationToken);
    }

    public async Task<Payment?> GetByMercadoPagoPaymentIdAsync(string mercadoPagoPaymentId, CancellationToken cancellationToken)
    {
        return await dbContext.Payments.FirstOrDefaultAsync(x => x.MercadoPagoPaymentId == mercadoPagoPaymentId, cancellationToken);
    }
}
