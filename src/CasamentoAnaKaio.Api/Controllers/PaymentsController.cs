using System.Text.Json;
using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Contracts.Payments;
using Microsoft.AspNetCore.Mvc;

namespace CasamentoAnaKaio.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(
    PaymentService paymentService,
    PaymentWebhookService webhookService) : ControllerBase
{
    [HttpPost("create")]
    public async Task<ActionResult<CreatePaymentResponse>> Create(
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await paymentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetStatus), new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<PaymentStatusResponse>> GetStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await paymentService.GetStatusAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("webhook/mercadopago")]
    public async Task<IActionResult> MercadoPagoWebhook(
        [FromHeader(Name = "x-signature")] string? signature,
        [FromHeader(Name = "x-request-id")] string? requestId,
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        var processed = await webhookService.ProcessWebhookAsync(
            payload.GetRawText(),
            signature ?? string.Empty,
            requestId ?? string.Empty,
            Request.Query.ToDictionary(x => x.Key, x => (string?)x.Value.ToString()),
            cancellationToken);

        return processed
            ? Ok(new { message = "Webhook processado com sucesso." })
            : BadRequest(new { message = "Webhook Mercado Pago invalido ou sem payment id." });
    }
}
