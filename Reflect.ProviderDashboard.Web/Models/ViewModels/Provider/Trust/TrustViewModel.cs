namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Trust;

public sealed class TrustViewModel
{
    public bool HttpsActive { get; set; }
    public bool HstsEnabled { get; set; }
    public string TimeZoneIana { get; set; } = string.Empty;
    public string TimeZoneLabel { get; set; } = string.Empty;
    public bool IntegrationEnabled { get; set; }
    public string IngestPath { get; set; } = "/api/v1/ingest/sessions";
}
