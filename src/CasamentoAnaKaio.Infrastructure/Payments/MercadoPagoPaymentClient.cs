using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Infrastructure.Options;

namespace CasamentoAnaKaio.Infrastructure.Payments;

public sealed class MercadoPagoPaymentClient(
    HttpClient httpClient,
    MercadoPagoOptions options) : IMercadoPagoPaymentClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<MercadoPagoPreferenceResult> CreateCheckoutPreferenceAsync(
        MercadoPagoPreferenceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        EnsureAccessToken();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "checkout/preferences");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        httpRequest.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(BuildPreferenceBody(request), options: JsonOptions);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Mercado Pago recusou a preferencia ({(int)response.StatusCode}): {payload}");
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        return new MercadoPagoPreferenceResult(
            ReadString(root, "id"),
            ReadString(root, "init_point"),
            ReadString(root, "sandbox_init_point"));
    }

    public async Task<MercadoPagoPaymentDetails> GetPaymentAsync(
        string mercadoPagoPaymentId,
        CancellationToken cancellationToken)
    {
        EnsureAccessToken();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"v1/payments/{mercadoPagoPaymentId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Mercado Pago nao retornou o pagamento {mercadoPagoPaymentId} ({(int)response.StatusCode}): {payload}");
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var transactionData = TryGet(root, "point_of_interaction", "transaction_data");

        return new MercadoPagoPaymentDetails(
            ReadString(root, "id"),
            ReadString(root, "status"),
            ReadOptionalString(root, "external_reference"),
            ReadOptionalString(root, "payment_method_id"),
            ReadOptionalString(root, "payment_type_id"),
            transactionData is null ? null : ReadOptionalString(transactionData.Value, "qr_code"),
            transactionData is null ? null : ReadOptionalString(transactionData.Value, "qr_code_base64"),
            transactionData is null ? null : ReadOptionalString(transactionData.Value, "ticket_url"));
    }

    private object BuildPreferenceBody(MercadoPagoPreferenceRequest request)
    {
        var frontendUrl = options.FrontendUrl.TrimEnd('/');
        var backendUrl = options.BackendUrl.TrimEnd('/');

        return new
        {
            items = new[]
            {
                new
                {
                    title = request.Title,
                    quantity = 1,
                    currency_id = "BRL",
                    unit_price = request.Amount
                }
            },
            payer = new
            {
                name = request.PayerName,
                email = request.PayerEmail
            },
            external_reference = request.ExternalReference,
            notification_url = $"{backendUrl}/api/payments/webhook/mercadopago",
            back_urls = new
            {
                success = $"{frontendUrl}/pagamento/sucesso",
                pending = $"{frontendUrl}/pagamento/pendente",
                failure = $"{frontendUrl}/pagamento/falha"
            },
            auto_return = "approved",
            statement_descriptor = "ANA E KAIO",
            payment_methods = new
            {
                excluded_payment_types = BuildExcludedPaymentTypes(request.PaymentMethod)
            },
            metadata = new
            {
                origin = "casamento_ana_kaio",
                payment_method = request.PaymentMethod
            }
        };
    }

    private static object[] BuildExcludedPaymentTypes(string paymentMethod)
    {
        var excluded = paymentMethod.ToLowerInvariant() switch
        {
            "pix" => ["credit_card", "debit_card", "ticket"],
            "boleto" => ["credit_card", "debit_card", "bank_transfer"],
            "credit_card" => ["ticket", "bank_transfer", "debit_card"],
            _ => Array.Empty<string>()
        };

        return excluded.Select(id => new { id }).ToArray();
    }

    private void EnsureAccessToken()
    {
        if (string.IsNullOrWhiteSpace(options.AccessToken) ||
            options.AccessToken.Contains("AQUI", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Configure MERCADOPAGO_ACCESS_TOKEN antes de criar pagamentos.");
        }
    }

    private static JsonElement? TryGet(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (!current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return ReadOptionalString(element, propertyName) ?? string.Empty;
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }
}
