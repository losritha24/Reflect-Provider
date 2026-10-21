using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Services;

public class InMemoryTherapySessionRepository : ITherapySessionRepository
{
    public Task<IReadOnlyList<TherapySession>> GetAllAsync(CancellationToken ct)
    {
        var list = InMemoryDataStore.Sessions
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToList();
        return Task.FromResult<IReadOnlyList<TherapySession>>(list);
    }

    public Task<IReadOnlyList<TherapySession>> GetByPatientIdAsync(Guid patientId, CancellationToken ct)
    {
        var list = InMemoryDataStore.Sessions
            .Where(s => s.PatientId == patientId && !s.IsDeleted)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToList();
        return Task.FromResult<IReadOnlyList<TherapySession>>(list);
    }

    public Task<int> CountSinceAsync(DateTimeOffset sinceUtc, CancellationToken ct)
    {
        var count = InMemoryDataStore.Sessions.Count(s => !s.IsDeleted && s.StartedAtUtc >= sinceUtc);
        return Task.FromResult(count);
    }

    public Task<int> CountByPlatformSinceAsync(TherapyPlatform platform, DateTimeOffset sinceUtc, CancellationToken ct)
    {
        var count = InMemoryDataStore.Sessions.Count(s =>
            !s.IsDeleted && s.Platform == platform && s.StartedAtUtc >= sinceUtc);
        return Task.FromResult(count);
    }
}
