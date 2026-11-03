using Reflect.ProviderDashboard.Domain.Entities;

namespace Reflect.ProviderDashboard.Domain.Interfaces;

public interface ICareDeviceRepository
{
    Task<IReadOnlyList<CareDevice>> GetAllAsync(CancellationToken ct);
    Task<CareDevice> GetByIdAsync(Guid deviceId, CancellationToken ct);
    Task<IReadOnlyList<CareDevice>> GetByPatientIdAsync(Guid patientId, CancellationToken ct);
    Task<IReadOnlyList<DeviceAssignment>> GetHistoryAsync(Guid deviceId, CancellationToken ct);
    Task ReassignAsync(Guid deviceId, Guid? newPatientId, CancellationToken ct);
}
