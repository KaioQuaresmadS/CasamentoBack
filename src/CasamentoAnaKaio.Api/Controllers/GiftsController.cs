using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Contracts.Gifts;
using Microsoft.AspNetCore.Mvc;

namespace CasamentoAnaKaio.Api.Controllers;

[ApiController]
[Route("api/gifts")]
public sealed class GiftsController(GiftService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GiftResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await service.ListActiveAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GiftResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var gift = await service.GetByIdAsync(id, cancellationToken);
        return gift is null ? NotFound() : Ok(gift);
    }
}
