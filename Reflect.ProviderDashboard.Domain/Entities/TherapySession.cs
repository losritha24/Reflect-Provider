using Reflect.ProviderDashboard.Domain.Enums;

namespace Reflect.ProviderDashboard.Domain.Entities;

public class TherapySession : BaseEntity
{
    public Guid PatientId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public TherapyPlatform Platform { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ReflectionSummary { get; set; } = string.Empty;
}
