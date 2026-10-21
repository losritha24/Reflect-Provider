using Reflect.ProviderDashboard.Domain.Interfaces;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;

public class PatientIndexViewModel
{
    public List<PatientRowViewModel> Rows { get; set; } = new();

    public async Task PrepareData(
        IPatientRepository patients,
        ITherapySessionRepository sessions,
        CancellationToken ct)
    {
        var allPatients = await patients.GetAllAsync(ct);
        var allSessions = await sessions.GetAllAsync(ct);

        Rows = allPatients
            .Select(p =>
            {
                var patientSessions = allSessions.Where(s => s.PatientId == p.Id).ToList();
                return PatientRowViewModel.FromEntity(p, patientSessions);
            })
            .OrderBy(r => r.DisplayName)
            .ToList();
    }
}
