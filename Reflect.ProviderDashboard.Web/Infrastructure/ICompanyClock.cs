namespace Reflect.ProviderDashboard.Web.Infrastructure;

public interface ICompanyClock
{
    string IanaId { get; }
    string Abbreviation { get; }
    DateTimeOffset ToUtc(DateTimeOffset value);
    DateTimeOffset ToCompany(DateTimeOffset value);
    string FormatDate(DateTimeOffset value);
    string FormatDateTime(DateTimeOffset value);
    string ZoneAbbreviation(DateTimeOffset value);
}
