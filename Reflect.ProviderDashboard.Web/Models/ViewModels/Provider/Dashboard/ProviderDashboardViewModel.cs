using Reflect.ProviderDashboard.Domain.Interfaces;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Dashboard;

public class ProviderDashboardViewModel
{
    public int ActivePatientCount { get; set; }
    public int SessionsLast7Days { get; set; }
    public int VrSessionsLast7Days { get; set; }
    public int MobileSessionsLast7Days { get; set; }
    public List<RecentSessionRowViewModel> RecentSessions { get; set; } = new();

    public async Task PrepareData(
        IPatientRepository patients,
        ITherapySessionRepository sessions,
        CancellationToken ct)
    {
        var allPatients = await patients.GetAllAsync(ct);
        ActivePatientCount = allPatients.Count;

        var since = DateTimeOffset.UtcNow.AddDays(-7);
        SessionsLast7Days = await sessions.CountSinceAsync(since, ct);
        VrSessionsLast7Days = await sessions.CountByPlatformSinceAsync(Domain.Enums.TherapyPlatform.Vr, since, ct);
        MobileSessionsLast7Days = await sessions.CountByPlatformSinceAsync(Domain.Enums.TherapyPlatform.Mobile, since, ct);

        var allSessions = await sessions.GetAllAsync(ct);
        var patientLookup = allPatients.ToDictionary(p => p.Id, p => p.DisplayName);

        RecentSessions = allSessions
            .Take(8)
            .Select(s => RecentSessionRowViewModel.FromEntity(s, patientLookup.GetValueOrDefault(s.PatientId, "Unknown")))
            .ToList();
    }
}
