namespace Reflect.ProviderDashboard.Domain.Entities;

public class DeviceAssignment : BaseEntity
{
    public Guid DeviceId { get; set; }
    public Guid? PatientId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
    public DateTimeOffset? UnassignedAtUtc { get; set; }
}
