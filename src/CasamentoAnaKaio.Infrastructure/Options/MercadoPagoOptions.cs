namespace CasamentoAnaKaio.Infrastructure.Options;

public sealed class MercadoPagoOptions
{
    public string AccessToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool IsSandbox { get; set; } = true;
    public string ExternalPosId { get; set; } = "CASAMENTO_ANA_KAIO";
}
