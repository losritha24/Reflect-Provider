using System.Globalization;

namespace Reflect.ProviderDashboard.Web.Extensions;

public static class DateTimeDisplayExtensions
{
    private static readonly TimeZoneInfo Eastern =
        TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static string ToUsDateString(this DateTimeOffset value)
    {
        var eastern = TimeZoneInfo.ConvertTime(value, Eastern);
        return eastern.ToString("M/d/yyyy", CultureInfo.InvariantCulture);
    }

    public static string ToUsDateTimeString(this DateTimeOffset value)
    {
        var eastern = TimeZoneInfo.ConvertTime(value, Eastern);
        return eastern.ToString("M/d/yyyy h:mm tt", CultureInfo.InvariantCulture) + " " + ZoneAbbreviation(value);
    }

    /// <summary>EST or EDT for the given instant, so every displayed time names its zone.</summary>
    public static string ZoneAbbreviation(DateTimeOffset value) =>
        Eastern.IsDaylightSavingTime(value) ? "EDT" : "EST";
}
