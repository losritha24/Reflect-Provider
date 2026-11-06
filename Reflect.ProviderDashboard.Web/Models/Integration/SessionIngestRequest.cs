namespace Reflect.ProviderDashboard.Web.Models.Integration;

public sealed class SessionIngestRequest
{
    public string ExternalPatientId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public int DurationMinutes { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string ReflectionSummary { get; set; } = string.Empty;
    public int? AffectScore { get; set; }
    public string HardwareId { get; set; }
}
