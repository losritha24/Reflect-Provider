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
        return eastern.ToString("M/d/yyyy h:mm tt", CultureInfo.InvariantCulture);
    }
}
