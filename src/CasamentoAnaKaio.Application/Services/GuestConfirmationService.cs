using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Contracts.GuestConfirmations;
using CasamentoAnaKaio.Domain.Entities;

namespace CasamentoAnaKaio.Application.Services;

public sealed class GuestConfirmationService(
    IGuestConfirmationRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<GuestConfirmationResponse> CreateAsync(
        CreateGuestConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var confirmation = new GuestConfirmation(
            request.FullName,
            request.Phone,
            request.GuestsCount,
            request.WillAttend,
            request.Notes);

        await repository.AddAsync(confirmation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(confirmation);
    }

    public async Task<IReadOnlyList<GuestConfirmationResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var confirmations = await repository.ListAsync(cancellationToken);
        return confirmations.Select(Map).ToList();
    }

    private static GuestConfirmationResponse Map(GuestConfirmation confirmation)
    {
        return new GuestConfirmationResponse(
            confirmation.Id,
            confirmation.FullName,
            confirmation.Phone,
            confirmation.GuestsCount,
            confirmation.WillAttend,
            confirmation.Notes,
            confirmation.CreatedAt);
    }
}
