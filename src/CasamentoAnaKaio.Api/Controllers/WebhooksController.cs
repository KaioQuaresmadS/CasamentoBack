using CasamentoAnaKaio.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasamentoAnaKaio.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public sealed class WebhooksController(PaymentWebhookService webhookService) : ControllerBase
{
    [HttpPost("mercado-pago/payments")]
    public async Task<IActionResult> MercadoPagoPaymentWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        var processed = await webhookService.ProcessWebhookAsync(
            payload,
            Request.Headers["x-signature"].ToString() ?? string.Empty,
            Request.Headers["x-request-id"].ToString() ?? string.Empty,
            Request.Query.ToDictionary(item => item.Key, item => (string?)item.Value.ToString()),
            cancellationToken);

        return processed ? Ok(new { message = "Webhook processado." }) : BadRequest(new { message = "Webhook ignorado." });
    }
}
