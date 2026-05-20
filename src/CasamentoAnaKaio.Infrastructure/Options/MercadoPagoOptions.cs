namespace CasamentoAnaKaio.Infrastructure.Options;

public sealed class MercadoPagoOptions
{
    public string AccessToken { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox";
    public string FrontendUrl { get; set; } = "http://localhost:4200";
    public string BackendUrl { get; set; } = "http://localhost:5278";

    public bool IsSandbox => Environment.Equals("sandbox", StringComparison.OrdinalIgnoreCase);
}
