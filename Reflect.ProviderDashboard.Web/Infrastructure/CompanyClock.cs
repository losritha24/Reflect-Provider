using System.Globalization;
using Microsoft.Extensions.Options;

namespace Reflect.ProviderDashboard.Web.Infrastructure;

public sealed class CompanyClock : ICompanyClock
{
    public static ICompanyClock Current { get; private set; } = new CompanyClock("America/New_York", "ET");

    private readonly TimeZoneInfo _zone;

    public CompanyClock(IOptions<CompanyOptions> options)
        : this(options.Value.TimeZone, options.Value.TimeZoneLabel)
    {
    }

    public CompanyClock(string ianaId, string abbreviation)
    {
        IanaId = string.IsNullOrWhiteSpace(ianaId) ? "America/New_York" : ianaId.Trim();
        Abbreviation = string.IsNullOrWhiteSpace(abbreviation) ? "ET" : abbreviation.Trim();
        _zone = Resolve(IanaId);
        Current = this;
    }

    public string IanaId { get; }
    public string Abbreviation { get; }

    public DateTimeOffset ToUtc(DateTimeOffset value) => value.ToUniversalTime();

    public DateTimeOffset ToCompany(DateTimeOffset value) => TimeZoneInfo.ConvertTime(ToUtc(value), _zone);

    public string FormatDate(DateTimeOffset value) =>
        ToCompany(value).ToString("M/d/yyyy", CultureInfo.InvariantCulture);

    public string FormatDateTime(DateTimeOffset value) =>
        ToCompany(value).ToString("M/d/yyyy h:mm tt", CultureInfo.InvariantCulture) + " " + ZoneAbbreviation(value);

    public string ZoneAbbreviation(DateTimeOffset value)
    {
        if (string.Equals(IanaId, "America/New_York", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Abbreviation, "ET", StringComparison.OrdinalIgnoreCase))
            return _zone.IsDaylightSavingTime(ToUtc(value)) ? "EDT" : "EST";
        return Abbreviation;
    }

    public DateTimeOffset StartOfCompanyDay(DateOnly date)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, _zone), TimeSpan.Zero);
    }

    private static TimeZoneInfo Resolve(string ianaId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ianaId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            if (string.Equals(ianaId, "America/New_York", StringComparison.OrdinalIgnoreCase))
                return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            throw new InvalidOperationException(
                $"Company time zone '{ianaId}' is not a valid IANA or Windows zone. Store UTC in RDS; set Company:TimeZone to the facility display zone.");
        }
    }
}
