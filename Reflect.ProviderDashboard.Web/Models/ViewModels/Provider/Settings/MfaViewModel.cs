namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Settings;

public sealed class MfaViewModel
{
    public bool Enabled { get; set; }
    public string Key { get; set; } = "";
    public string Uri { get; set; } = "";
    public IReadOnlyList<string> RecoveryCodes { get; set; } = [];
    public int RecoveryCodesLeft { get; set; }
    public string Error { get; set; }
}
