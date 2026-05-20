using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Infrastructure.Options;

namespace CasamentoAnaKaio.Infrastructure.Payments;

public sealed class MercadoPagoPaymentClient(HttpClient httpClient, MercadoPagoOptions options) : IMercadoPagoPaymentClient
{
    public async Task<MercadoPagoPreferenceResult> CreatePreferenceAsync(
        MercadoPagoPreferenceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        EnsureToken();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "checkout/preferences");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        httpRequest.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(BuildPreferenceBody(request));

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Mercado Pago preference error: {(int)response.StatusCode} {json}");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return new MercadoPagoPreferenceResult(
            ReadString(root, "id"),
            ReadString(root, "init_point"),
            ReadString(root, "sandbox_init_point"));
    }

    public async Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
    {
        EnsureToken();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"v1/payments/{Uri.EscapeDataString(paymentId)}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Mercado Pago payment lookup error: {(int)response.StatusCode} {json}");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var transactionData = root.TryGetProperty("point_of_interaction", out var pointOfInteraction)
            && pointOfInteraction.TryGetProperty("transaction_data", out var data)
            ? data
            : default;

        return new MercadoPagoPaymentDetails(
            ReadString(root, "id"),
            ReadString(root, "status"),
            ReadString(root, "external_reference"),
            ReadString(root, "payment_method_id"),
            ReadDecimal(root, "transaction_amount"),
            transactionData.ValueKind == JsonValueKind.Object ? ReadString(transactionData, "qr_code_base64") : null,
            transactionData.ValueKind == JsonValueKind.Object ? ReadString(transactionData, "qr_code") : null);
    }

    private static object BuildPreferenceBody(MercadoPagoPreferenceRequest request)
    {
        return new
        {
            items = new[]
            {
                new
                {
                    title = request.Title,
                    quantity = 1,
                    unit_price = request.Amount,
                    currency_id = "BRL"
                }
            },
            payer = new
            {
                name = request.PayerName,
                email = request.PayerEmail
            },
            external_reference = request.ExternalReference,
            notification_url = request.NotificationUrl,
            back_urls = new
            {
                success = request.SuccessUrl,
                failure = request.FailureUrl,
                pending = request.PendingUrl
            },
            auto_return = "approved",
            statement_descriptor = "ANA E KAIO",
            payment_methods = BuildPaymentMethodFilter(request.PaymentMethod)
        };
    }

    private static object BuildPaymentMethodFilter(string paymentMethod)
    {
        return paymentMethod switch
        {
            "pix" => new
            {
                excluded_payment_types = new[]
                {
                    new { id = "credit_card" },
                    new { id = "debit_card" },
                    new { id = "ticket" }
                },
                installments = 1
            },
            "boleto" => new
            {
                excluded_payment_types = new[]
                {
                    new { id = "credit_card" },
                    new { id = "debit_card" },
                    new { id = "bank_transfer" }
                },
                installments = 1
            },
            "credit_card" => new
            {
                excluded_payment_types = new[]
                {
                    new { id = "ticket" },
                    new { id = "bank_transfer" }
                },
                installments = 12
            },
            _ => new { installments = 12 }
        };
    }

    private void EnsureToken()
    {
        if (string.IsNullOrWhiteSpace(options.AccessToken) || options.AccessToken.Contains("_AQUI", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("MERCADOPAGO_ACCESS_TOKEN nao configurado.");
        }
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.GetRawText(),
            _ => string.Empty
        };
    }

    private static decimal ReadDecimal(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.TryGetDecimal(out var value)
            ? value
            : 0m;
    }
}
