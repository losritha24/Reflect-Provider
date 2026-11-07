using Reflect.ProviderDashboard.Domain.Catalog;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Dashboard;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Digest;

public sealed record ProgramCount(string Group, int Sessions);

/// <summary>One-page summary of the last seven days for the signed-in provider's caseload.</summary>
public sealed class DigestViewModel
{
    public ProviderDashboardViewModel Snapshot { get; private set; } = new();
    public IReadOnlyList<DueFollowUp> Due { get; private set; } = [];
    public IReadOnlyList<ProgramCount> Programs { get; private set; } = [];
    public DateTimeOffset From { get; private set; }
    public DateTimeOffset To { get; private set; }

    public async Task PrepareData(IPatientRepository patients, ITherapySessionRepository sessions, FollowUpService followUps, CancellationToken ct)
    {
        Snapshot = new ProviderDashboardViewModel();
        await Snapshot.PrepareData(patients, sessions, ct);
        To = Snapshot.AsOfUtc;
        From = To.AddDays(-7);
        Due = followUps is null ? [] : await followUps.DueAsync(7, ct);
        Programs = (await sessions.GetAllAsync(ct))
            .Where(s => s.StartedAtUtc >= From && s.StartedAtUtc <= To)
            .GroupBy(s => ReflectModuleCatalog.Find(s.Platform, s.ModuleName)?.Group ?? "Other")
            .Select(g => new ProgramCount(g.Key, g.Count()))
            .OrderByDescending(p => p.Sessions).ThenBy(p => p.Group).ToList();
    }
}
