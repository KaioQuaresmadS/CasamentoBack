using CasamentoAnaKaio.Domain.Entities;

namespace CasamentoAnaKaio.Application.Abstractions;

public interface IGuestConfirmationRepository
{
    Task AddAsync(GuestConfirmation confirmation, CancellationToken cancellationToken);
    Task<IReadOnlyList<GuestConfirmation>> ListAsync(CancellationToken cancellationToken);
}
