using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Contracts.GuestConfirmations;
using Microsoft.AspNetCore.Mvc;

namespace CasamentoAnaKaio.Api.Controllers;

[ApiController]
[Route("api/guest-confirmations")]
public sealed class GuestConfirmationsController(GuestConfirmationService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<GuestConfirmationResponse>> Create(
        CreateGuestConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GuestConfirmationResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await service.ListAsync(cancellationToken));
    }
}
