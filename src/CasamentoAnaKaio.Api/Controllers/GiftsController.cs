using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Contracts.Gifts;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GiftResponse>> Create(
        CreateGiftRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GiftResponse>> Update(
        Guid id,
        UpdateGiftRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.UpdateAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var removed = await service.DeleteAsync(id, cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}
