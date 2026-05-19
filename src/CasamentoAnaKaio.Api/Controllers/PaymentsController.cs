using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Contracts.GiftContributions;
using Microsoft.AspNetCore.Mvc;

namespace CasamentoAnaKaio.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(GiftContributionService service) : ControllerBase
{
    [HttpPost("{contributionId:guid}/simulate-success")]
    public async Task<ActionResult<PaymentStatusResponse>> SimulateSuccess(
        Guid contributionId,
        CancellationToken cancellationToken)
    {
        var response = await service.MarkAsPaidAsync(contributionId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{contributionId:guid}/simulate-failure")]
    public async Task<ActionResult<PaymentStatusResponse>> SimulateFailure(
        Guid contributionId,
        CancellationToken cancellationToken)
    {
        var response = await service.MarkAsFailedAsync(contributionId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
