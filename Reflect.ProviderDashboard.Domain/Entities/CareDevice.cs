using Reflect.ProviderDashboard.Domain.Enums;

namespace Reflect.ProviderDashboard.Domain.Entities;

public class CareDevice : BaseEntity
{
    public string DisplayName { get; set; } = string.Empty;
    public DeviceKind Kind { get; set; }
    public string HardwareId { get; set; } = string.Empty;
    public Guid? CurrentPatientId { get; set; }
}
