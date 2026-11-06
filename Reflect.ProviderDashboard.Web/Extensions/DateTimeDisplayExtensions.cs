using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Extensions;

public static class DateTimeDisplayExtensions
{
    public static string ToUsDateString(this DateTimeOffset value) =>
        CompanyClock.Current.FormatDate(value);

    public static string ToUsDateTimeString(this DateTimeOffset value) =>
        CompanyClock.Current.FormatDateTime(value);
}
