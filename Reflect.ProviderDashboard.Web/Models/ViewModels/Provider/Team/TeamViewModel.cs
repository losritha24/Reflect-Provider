using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Team;

public sealed record TeamMember(string Id, string DisplayName, string Email, string Role, bool Active, int PatientCount, bool IsSelf);

public sealed class TeamViewModel
{
    public IReadOnlyList<TeamMember> Providers { get; set; } = [];
    public IReadOnlyList<Invite> Invites { get; set; } = [];
    public IReadOnlyList<AssignablePatient> Patients { get; set; } = [];
    public IReadOnlyList<AuditEntry> Audit { get; set; } = [];
    public DateTimeOffset Now { get; set; }
    public string NewInviteLink { get; set; }
    public string NewInviteEmail { get; set; }
    public string Error { get; set; }

    public string ProviderLabel(string providerId) =>
        string.IsNullOrEmpty(providerId) ? "Unassigned"
        : Providers.FirstOrDefault(p => p.Id == providerId)?.DisplayName ?? "Unknown provider";
}
