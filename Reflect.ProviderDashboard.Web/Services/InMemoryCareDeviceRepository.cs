using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Services;

public class InMemoryCareDeviceRepository : ICareDeviceRepository
{
    private readonly IPatientRepository _patients;

    public InMemoryCareDeviceRepository(IPatientRepository patients)
    {
        _patients = patients;
    }

    public async Task<IReadOnlyList<CareDevice>> GetAllAsync(CancellationToken ct)
    {
        var accessible = await AccessiblePatientIds(ct);
        return InMemoryDataStore.Devices
            .Where(device => !device.IsDeleted && DeviceIsVisible(device, accessible))
            .OrderBy(device => device.DisplayName)
            .ToList();
    }

    public async Task<CareDevice> GetByIdAsync(Guid deviceId, CancellationToken ct)
    {
        var match = (await GetAllAsync(ct)).FirstOrDefault(device => device.Id == deviceId);
        if (match is null)
            throw new EntityNotFoundException("Device not found.");
        return match;
    }

    public async Task<IReadOnlyList<CareDevice>> GetByPatientIdAsync(Guid patientId, CancellationToken ct)
    {
        await _patients.GetByIdAsync(patientId, ct);
        return InMemoryDataStore.Devices
            .Where(device => !device.IsDeleted && device.CurrentPatientId == patientId)
            .OrderBy(device => device.DisplayName)
            .ToList();
    }

    public async Task<IReadOnlyList<DeviceAssignment>> GetHistoryAsync(Guid deviceId, CancellationToken ct)
    {
        await GetByIdAsync(deviceId, ct);
        return InMemoryDataStore.DeviceAssignments
            .Where(assignment => assignment.DeviceId == deviceId && !assignment.IsDeleted)
            .OrderByDescending(assignment => assignment.AssignedAtUtc)
            .ToList();
    }

    public async Task ReassignAsync(Guid deviceId, Guid? newPatientId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var device = await GetByIdAsync(deviceId, ct);
        if (newPatientId.HasValue)
            await _patients.GetByIdAsync(newPatientId.Value, ct);

        var now = DateTimeOffset.UtcNow;
        var open = InMemoryDataStore.DeviceAssignments.FirstOrDefault(assignment =>
            assignment.DeviceId == deviceId && assignment.UnassignedAtUtc is null && !assignment.IsDeleted);
        if (open is not null)
            open.UnassignedAtUtc = now;

        InMemoryDataStore.DeviceAssignments.Add(new DeviceAssignment
        {
            DeviceId = deviceId,
            PatientId = newPatientId,
            AssignedAtUtc = now
        });
        device.CurrentPatientId = newPatientId;
    }

    private async Task<HashSet<Guid>> AccessiblePatientIds(CancellationToken ct)
    {
        return (await _patients.GetAllAsync(ct)).Select(patient => patient.Id).ToHashSet();
    }

    private static bool DeviceIsVisible(CareDevice device, HashSet<Guid> accessible)
    {
        return device.CurrentPatientId is null || accessible.Contains(device.CurrentPatientId.Value);
    }
}
