using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Settings;

public sealed class SettingsViewModel
{
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string FacilityName { get; set; } = "";
    public string FacilityTimeZone { get; set; } = "";
    public bool IsAdmin { get; set; }
    public ProviderPreferences Prefs { get; set; } = new();
    public string Message { get; set; }
    public string Error { get; set; }
    public IReadOnlyList<AuditEntry> Activity { get; set; } = [];
}
