using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Contracts.GiftContributions;
using Microsoft.AspNetCore.Mvc;

namespace CasamentoAnaKaio.Api.Controllers;

[ApiController]
[Route("api/gift-contributions")]
public sealed class GiftContributionsController(GiftContributionService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<GiftContributionResponse>> Create(
        CreateGiftContributionRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return StatusCode(
            StatusCodes.Status410Gone,
            new { message = "Use /api/payments/create para criar pagamentos via Mercado Pago." });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GiftContributionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await service.GetByIdAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<PaymentStatusResponse>> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var response = await service.GetStatusAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
