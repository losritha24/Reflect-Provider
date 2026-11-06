namespace Reflect.ProviderDashboard.Web.Infrastructure;

public sealed class CompanyOptions
{
    public const string SectionName = "Company";

    /// <summary>IANA time zone used for display. Storage remains UTC (RDS timestamptz).</summary>
    public string TimeZone { get; set; } = "America/New_York";

    /// <summary>Short label shown in the UI, e.g. ET.</summary>
    public string TimeZoneLabel { get; set; } = "ET";
}
