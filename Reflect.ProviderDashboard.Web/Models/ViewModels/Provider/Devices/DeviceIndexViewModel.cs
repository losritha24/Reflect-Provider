using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Extensions;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Devices;

public class DeviceIndexViewModel
{
    public List<DeviceRow> Rows { get; set; } = new();
    public List<Patient> Patients { get; set; } = new();

    public async Task PrepareData(ICareDeviceRepository devices, IPatientRepository patients, CancellationToken ct)
    {
        Patients = (await patients.GetAllAsync(ct)).OrderBy(patient => patient.DisplayName).ToList();
        var names = Patients.ToDictionary(patient => patient.Id, patient => patient.DisplayName);
        var all = await devices.GetAllAsync(ct);
        Rows = new List<DeviceRow>(all.Count);
        foreach (var device in all)
        {
            var history = await devices.GetHistoryAsync(device.Id, ct);
            Rows.Add(new DeviceRow
            {
                Device = device,
                CurrentPatientName = device.CurrentPatientId is Guid id && names.TryGetValue(id, out var name)
                    ? name
                    : "Unassigned",
                History = history.Select(assignment => new AssignmentRow
                {
                    PatientName = assignment.PatientId is Guid patientId && names.TryGetValue(patientId, out var assigned)
                        ? assigned
                        : "Unassigned / storage",
                    AssignedDisplay = assignment.AssignedAtUtc.ToUsDateTimeString(),
                    UnassignedDisplay = assignment.UnassignedAtUtc?.ToUsDateTimeString()
                }).ToList()
            });
        }
    }

    public class DeviceRow
    {
        public CareDevice Device { get; set; }
        public string CurrentPatientName { get; set; } = string.Empty;
        public List<AssignmentRow> History { get; set; } = new();
    }

    public class AssignmentRow
    {
        public string PatientName { get; set; } = string.Empty;
        public string AssignedDisplay { get; set; } = string.Empty;
        public string UnassignedDisplay { get; set; }
    }
}
