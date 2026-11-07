using System.Security.Claims;

namespace Reflect.ProviderDashboard.Web.Services;

/// <summary>
/// Per-account preferences kept as Identity user claims (no schema change). They are copied into
/// the sign-in cookie, so saving settings re-issues the session. Nothing here is clinical data.
/// </summary>
public sealed class ProviderPreferences
{
    public const string Prefix = "pref.";

    public static readonly string[] Themes = ["", "light", "dark"];
    public static readonly string[] TextSizes = ["", "100%", "112.5%", "125%"];
    public static readonly string[] LandingPages = ["Dashboard", "Patients", "Graph", "Messages"];
    public static readonly string[] Channels = ["in-app", "email", "push"];
    public static readonly string[] Clocks = ["12h", "24h"];

    public string Title { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Theme { get; set; } = "";
    public string TextSize { get; set; } = "";
    public string LandingPage { get; set; } = "Dashboard";
    public bool ShowTour { get; set; }
    public string TimeZone { get; set; } = "";
    public string Clock { get; set; } = "12h";
    public bool NotifyQuieter { get; set; } = true;
    public bool NotifyBaseline { get; set; } = true;
    public bool NotifyMessages { get; set; } = true;
    public string Channel { get; set; } = "in-app";
    public string QuietFrom { get; set; } = "";
    public string QuietTo { get; set; } = "";

    public static ProviderPreferences From(IEnumerable<Claim> claims)
    {
        var map = claims.Where(c => c.Type.StartsWith(Prefix, StringComparison.Ordinal))
            .GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.First().Value);
        string Get(string key, string fallback = "") => map.TryGetValue(Prefix + key, out var v) ? v : fallback;
        bool Flag(string key, bool fallback) => map.TryGetValue(Prefix + key, out var v) ? v == "1" : fallback;
        return new ProviderPreferences
        {
            Title = Get("title"), Phone = Get("phone"), Theme = Pick(Get("theme"), Themes, ""),
            TextSize = Pick(Get("textsize"), TextSizes, ""), LandingPage = Pick(Get("landing"), LandingPages, "Dashboard"),
            ShowTour = Flag("showtour", false), TimeZone = Get("timezone"), Clock = Pick(Get("clock"), Clocks, "12h"),
            NotifyQuieter = Flag("n.quieter", true), NotifyBaseline = Flag("n.baseline", true), NotifyMessages = Flag("n.messages", true),
            Channel = Pick(Get("channel"), Channels, "in-app"), QuietFrom = Get("quietfrom"), QuietTo = Get("quietto")
        };
    }

    public IEnumerable<Claim> ToClaims()
    {
        Claim C(string key, string value) => new(Prefix + key, value ?? "");
        yield return C("title", Trim(Title, 80));
        yield return C("phone", Trim(Phone, 32));
        yield return C("theme", Pick(Theme, Themes, ""));
        yield return C("textsize", Pick(TextSize, TextSizes, ""));
        yield return C("landing", Pick(LandingPage, LandingPages, "Dashboard"));
        yield return C("showtour", ShowTour ? "1" : "0");
        yield return C("timezone", Trim(TimeZone, 64));
        yield return C("clock", Pick(Clock, Clocks, "12h"));
        yield return C("n.quieter", NotifyQuieter ? "1" : "0");
        yield return C("n.baseline", NotifyBaseline ? "1" : "0");
        yield return C("n.messages", NotifyMessages ? "1" : "0");
        yield return C("channel", Pick(Channel, Channels, "in-app"));
        yield return C("quietfrom", ValidTime(QuietFrom));
        yield return C("quietto", ValidTime(QuietTo));
    }

    /// <summary>Changes whenever display-affecting values change, so the browser re-applies them once.</summary>
    public string DisplayVersion => $"{Theme}|{TextSize}|{(ShowTour ? "t" : "")}";

    private static string Pick(string value, string[] allowed, string fallback) =>
        allowed.Contains(value ?? "", StringComparer.Ordinal) ? value ?? "" : fallback;
    private static string Trim(string value, int max) => (value ?? "").Trim() is var t && t.Length > max ? t[..max] : (value ?? "").Trim();
    private static string ValidTime(string value) =>
        TimeOnly.TryParseExact(value ?? "", "HH:mm", null, System.Globalization.DateTimeStyles.None, out var t) ? t.ToString("HH:mm") : "";
}
