using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Services;

public class InMemoryTherapySessionRepository(IPatientRepository patients) : ITherapySessionRepository
{
    private async Task<IEnumerable<TherapySession>> AccessibleSessions(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ids = (await patients.GetAllAsync(ct)).Select(p => p.Id).ToHashSet();
        return InMemoryDataStore.Sessions.Where(s => !s.IsDeleted && ids.Contains(s.PatientId));
    }

    public async Task<IReadOnlyList<TherapySession>> GetAllAsync(CancellationToken ct) =>
        (await AccessibleSessions(ct)).OrderByDescending(s => s.StartedAtUtc).ToList();

    public async Task<IReadOnlyList<TherapySession>> GetByPatientIdAsync(Guid patientId, CancellationToken ct)
    {
        await patients.GetByIdAsync(patientId, ct);
        return InMemoryDataStore.Sessions.Where(s => !s.IsDeleted && s.PatientId == patientId)
            .OrderByDescending(s => s.StartedAtUtc).ToList();
    }

    public async Task<int> CountSinceAsync(DateTimeOffset sinceUtc, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return (await AccessibleSessions(ct)).Count(s => s.StartedAtUtc >= sinceUtc && s.StartedAtUtc <= now);
    }

    public async Task<int> CountByPlatformSinceAsync(TherapyPlatform platform, DateTimeOffset sinceUtc, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return (await AccessibleSessions(ct)).Count(s => s.Platform == platform
            && s.StartedAtUtc >= sinceUtc && s.StartedAtUtc <= now);
    }
}
