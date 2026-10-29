using Reflect.ProviderDashboard.Domain.Enums;

namespace Reflect.ProviderDashboard.Domain.Entities;

public class Patient : BaseEntity
{
    public string AssignedProviderId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ExternalPatientId { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public EngagementTrend Trend { get; set; } = EngagementTrend.Stable;
    public string NotesSummary { get; set; } = string.Empty;
}
