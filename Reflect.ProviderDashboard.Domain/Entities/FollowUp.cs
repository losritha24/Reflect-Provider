namespace Reflect.ProviderDashboard.Domain.Entities;

public enum FollowUpStatus
{
    Open = 1,
    Completed = 2,
    Dismissed = 3
}

/// <summary>A clinician's reminder to follow up on one patient. Text is bounded and never logged.</summary>
public class FollowUpTask : BaseEntity
{
    public Guid PatientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTimeOffset? DueAtUtc { get; set; }
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Open;
    /// <summary>Incremented on every change; a stale edit is refused instead of overwriting.</summary>
    public int Version { get; set; } = 1;
    public string CreatedByProviderId { get; set; } = string.Empty;
}

/// <summary>Append-only history written in the same transaction as the task change. Never updated or deleted.</summary>
public class FollowUpEvent : BaseEntity
{
    public Guid TaskId { get; set; }
    public Guid PatientId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorProviderId { get; set; } = string.Empty;
    public int VersionAfter { get; set; }
}
