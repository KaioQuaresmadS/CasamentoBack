using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Infrastructure.Options;
using Serilog;

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

        var requestBody = BuildPreferenceBody(request);
        var requestJson = JsonSerializer.Serialize(requestBody, JsonOptions);

        Log.Information(
            "Mercado Pago Checkout Pro preference request. Endpoint={Endpoint}, ExternalReference={ExternalReference}, RequestJson={RequestJson}",
            "POST /checkout/preferences",
            request.ExternalReference,
            requestJson);

        httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        LogPreferenceResponse(response, payload, request.ExternalReference);

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

    public async Task<MercadoPagoPaymentDetails> CreatePixPaymentAsync(
        MercadoPagoPixPaymentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        EnsureAccessToken();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/payments");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        httpRequest.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(BuildPixPaymentBody(request), options: JsonOptions);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Log.Error(
                "Mercado Pago recusou Pix. StatusCode={StatusCode}, ExternalReference={ExternalReference}, Response={Response}",
                (int)response.StatusCode,
                request.ExternalReference,
                payload);

            throw new InvalidOperationException($"Mercado Pago recusou o pagamento Pix ({(int)response.StatusCode}): {payload}");
        }

        return ParsePaymentDetails(payload);
    }

    public async Task<MercadoPagoPaymentDetails> CreateBoletoPaymentAsync(
        MercadoPagoBoletoPaymentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        EnsureAccessToken();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/payments");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        httpRequest.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(BuildBoletoPaymentBody(request), options: JsonOptions);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Log.Error(
                "Mercado Pago recusou boleto. StatusCode={StatusCode}, ExternalReference={ExternalReference}, Response={Response}",
                (int)response.StatusCode,
                request.ExternalReference,
                payload);

            throw new InvalidOperationException($"Mercado Pago recusou o pagamento boleto ({(int)response.StatusCode}): {payload}");
        }

        return ParsePaymentDetails(payload);
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

        return ParsePaymentDetails(payload);
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
            payment_methods = new
            {
                excluded_payment_methods = Array.Empty<object>(),
                excluded_payment_types = Array.Empty<object>(),
                installments = 12
            }
        };
    }

    private object BuildPixPaymentBody(MercadoPagoPixPaymentRequest request)
    {
        var backendUrl = options.BackendUrl.TrimEnd('/');

        return new
        {
            transaction_amount = request.Amount,
            description = request.Description,
            payment_method_id = "pix",
            payer = new
            {
                email = request.PayerEmail,
                first_name = request.PayerName
            },
            external_reference = request.ExternalReference,
            notification_url = $"{backendUrl}/api/payments/webhook"
        };
    }

    private object BuildBoletoPaymentBody(MercadoPagoBoletoPaymentRequest request)
    {
        var backendUrl = options.BackendUrl.TrimEnd('/');

        return new
        {
            transaction_amount = request.Amount,
            description = request.Description,
            payment_method_id = "bolbradesco",
            payer = new
            {
                email = request.PayerEmail,
                first_name = request.PayerName
            },
            external_reference = request.ExternalReference,
            notification_url = $"{backendUrl}/api/payments/webhook"
        };
    }

    private static MercadoPagoPaymentDetails ParsePaymentDetails(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var transactionData = TryGet(root, "point_of_interaction", "transaction_data");
        var transactionDetails = TryGet(root, "transaction_details");

        return new MercadoPagoPaymentDetails(
            ReadString(root, "id"),
            ReadString(root, "status"),
            ReadOptionalString(root, "external_reference"),
            ReadOptionalString(root, "payment_method_id"),
            ReadOptionalString(root, "payment_type_id"),
            transactionData is null ? null : ReadOptionalString(transactionData.Value, "qr_code"),
            transactionData is null ? null : ReadOptionalString(transactionData.Value, "qr_code_base64"),
            FirstNotBlank(
                transactionData is null ? null : ReadOptionalString(transactionData.Value, "ticket_url"),
                transactionDetails is null ? null : ReadOptionalString(transactionDetails.Value, "external_resource_url"),
                transactionDetails is null ? null : ReadOptionalString(transactionDetails.Value, "ticket_url"),
                ReadOptionalString(root, "external_resource_url")),
            FirstNotBlank(
                transactionDetails is null ? null : ReadOptionalString(transactionDetails.Value, "barcode"),
                transactionDetails is null ? null : ReadOptionalString(transactionDetails.Value, "barcode_content"),
                ReadOptionalString(root, "barcode")),
            FirstNotBlank(
                transactionDetails is null ? null : ReadOptionalString(transactionDetails.Value, "payment_method_reference_id"),
                transactionDetails is null ? null : ReadOptionalString(transactionDetails.Value, "line"),
                transactionDetails is null ? null : ReadOptionalString(transactionDetails.Value, "linha_digitavel"),
                ReadOptionalString(root, "linha_digitavel")));
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

    private static void LogPreferenceResponse(
        HttpResponseMessage response,
        string payload,
        string externalReference)
    {
        string? preferenceId = null;
        string? initPoint = null;
        string? sandboxInitPoint = null;
        string? collectorId = null;
        string? clientId = null;
        string? applicationId = null;

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            preferenceId = ReadOptionalString(root, "id");
            initPoint = ReadOptionalString(root, "init_point");
            sandboxInitPoint = ReadOptionalString(root, "sandbox_init_point");
            collectorId = ReadOptionalString(root, "collector_id");
            clientId = ReadOptionalString(root, "client_id");
            applicationId = ReadOptionalString(root, "application_id");
        }
        catch (JsonException)
        {
            Log.Warning(
                "Mercado Pago Checkout Pro preference response is not valid JSON. StatusCode={StatusCode}, ExternalReference={ExternalReference}, ResponseBody={ResponseBody}",
                (int)response.StatusCode,
                externalReference,
                payload);
            return;
        }

        Log.Information(
            "Mercado Pago Checkout Pro preference response. StatusCode={StatusCode}, ExternalReference={ExternalReference}, ResponseBody={ResponseBody}, PreferenceId={PreferenceId}, InitPoint={InitPoint}, SandboxInitPoint={SandboxInitPoint}, CollectorId={CollectorId}, ClientId={ClientId}, ApplicationId={ApplicationId}",
            (int)response.StatusCode,
            externalReference,
            payload,
            preferenceId,
            initPoint,
            sandboxInitPoint,
            collectorId,
            clientId,
            applicationId);
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

    private static string? FirstNotBlank(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
