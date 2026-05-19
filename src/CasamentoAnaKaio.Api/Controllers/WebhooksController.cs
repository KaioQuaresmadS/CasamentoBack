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
    public async Task<IActionResult> MercadoPagoPaymentWebhook(
        [FromHeader(Name = "X-Signature")] string signature,
        [FromBody] object payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(signature))
        {
            return BadRequest("Assinatura de webhook não fornecida.");
        }

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
        var isValid = await webhookService.ValidateWebhookSignatureAsync(payloadJson, signature);

        if (!isValid)
        {
            return Unauthorized("Assinatura de webhook inválida.");
        }

        var processed = await webhookService.ProcessWebhookAsync(payloadJson, signature, cancellationToken);

        if (!processed)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao processar webhook.");
        }

        return Ok(new { message = "Webhook processado com sucesso." });
    }
}
