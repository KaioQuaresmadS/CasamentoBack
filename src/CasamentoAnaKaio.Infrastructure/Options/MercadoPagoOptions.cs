namespace CasamentoAnaKaio.Infrastructure.Options;

public sealed class MercadoPagoOptions
{
    public string AccessToken { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox";
    public bool IsSandbox { get; set; } = true;
    public string ExternalPosId { get; set; } = "CASAMENTO_ANA_KAIO";
    public string FrontendUrl { get; set; } = "http://localhost:4200";
    public string BackendUrl { get; set; } = "http://localhost:5278";
}
