using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Extensions;

public static class DateTimeDisplayExtensions
{
    public static string ToUsDateString(this DateTimeOffset value) =>
        CompanyClock.Current.FormatDate(value);

    public static string ToUsDateTimeString(this DateTimeOffset value) =>
        CompanyClock.Current.FormatDateTime(value);

    /// <summary>EST or EDT (or the configured company label) for the given instant.</summary>
    public static string ZoneAbbreviation(this DateTimeOffset value) =>
        CompanyClock.Current.ZoneAbbreviation(value);
}
