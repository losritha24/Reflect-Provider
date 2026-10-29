using Reflect.ProviderDashboard.Domain.Interfaces;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Dashboard;

public class ProviderDashboardViewModel
{
    public DateTimeOffset AsOfUtc { get; private set; }
    public List<AttentionRow> NeedsAttention { get; private set; } = new();
    public record AttentionRow(Guid PatientId, string PatientName, int CurrentSessions, int PreviousSessions, DateTimeOffset? LastActivity);
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

        AsOfUtc = DateTimeOffset.UtcNow;
        var since = AsOfUtc.AddDays(-7);
        SessionsLast7Days = await sessions.CountSinceAsync(since, ct);
        VrSessionsLast7Days = await sessions.CountByPlatformSinceAsync(Domain.Enums.TherapyPlatform.Vr, since, ct);
        MobileSessionsLast7Days = await sessions.CountByPlatformSinceAsync(Domain.Enums.TherapyPlatform.Mobile, since, ct);

        var allSessions = await sessions.GetAllAsync(ct);
        var patientLookup = allPatients.ToDictionary(p => p.Id, p => p.DisplayName);

        var activityLookup = allSessions.Where(s => s.StartedAtUtc <= AsOfUtc).ToLookup(s => s.PatientId);
        NeedsAttention = allPatients.Select(p =>
        {
            var activity = activityLookup[p.Id].ToList();
            return new AttentionRow(p.Id, p.DisplayName,
                activity.Count(s => s.StartedAtUtc >= since),
                activity.Count(s => s.StartedAtUtc >= since.AddDays(-7) && s.StartedAtUtc < since),
                activity.Count == 0 ? null : activity.Max(s => s.StartedAtUtc));
        }).Where(row => row.CurrentSessions < row.PreviousSessions)
          .OrderByDescending(row => row.PreviousSessions - row.CurrentSessions)
          .ThenBy(row => row.PatientName).ToList();

        RecentSessions = allSessions
            .Where(s => patientLookup.ContainsKey(s.PatientId) && s.StartedAtUtc <= AsOfUtc)
            .Take(8)
            .Select(s => RecentSessionRowViewModel.FromEntity(s, patientLookup.GetValueOrDefault(s.PatientId, "Unknown")))
            .ToList();
    }
}
