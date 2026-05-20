using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Contracts.Payments;
using CasamentoAnaKaio.Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasamentoAnaKaio.Api.Controllers;

[ApiController]
[Route("api/payments")]
[AllowAnonymous]
public sealed class PaymentsController(
    PaymentService paymentService,
    PaymentWebhookService webhookService,
    MercadoPagoOptions mercadoPagoOptions) : ControllerBase
{
    [HttpPost("create")]
    public async Task<ActionResult<CreatePaymentResponse>> Create(
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await paymentService.CreateAsync(
            request,
            mercadoPagoOptions.FrontendUrl,
            mercadoPagoOptions.BackendUrl,
            cancellationToken);

        return CreatedAtAction(nameof(GetStatus), new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<PaymentStatusResponse>> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var response = await paymentService.GetStatusAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("webhook/mercadopago")]
    public async Task<IActionResult> MercadoPagoWebhook(CancellationToken cancellationToken)
    {
        var payload = await ReadBodyAsync();
        var processed = await webhookService.ProcessWebhookAsync(
            payload,
            ReadHeaders(),
            ReadQuery(),
            cancellationToken);

        return processed ? Ok(new { message = "Webhook processado." }) : BadRequest(new { message = "Webhook ignorado." });
    }

    private async Task<string> ReadBodyAsync()
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync();
    }

    private Dictionary<string, string> ReadHeaders()
    {
        return Request.Headers.ToDictionary(
            header => header.Key.ToLowerInvariant(),
            header => header.Value.ToString());
    }

    private Dictionary<string, string> ReadQuery()
    {
        return Request.Query.ToDictionary(
            item => item.Key,
            item => item.Value.ToString());
    }
}
