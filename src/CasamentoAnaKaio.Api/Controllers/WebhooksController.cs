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
        var payload = await reader.ReadToEndAsync();
        var processed = await webhookService.ProcessWebhookAsync(
            payload,
            Request.Headers.ToDictionary(header => header.Key.ToLowerInvariant(), header => header.Value.ToString()),
            Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString()),
            cancellationToken);

        return processed ? Ok(new { message = "Webhook processado." }) : BadRequest(new { message = "Webhook ignorado." });
    }
}
