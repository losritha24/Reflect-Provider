using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Data;

namespace Reflect.ProviderDashboard.Web.Services;

public class EfTherapySessionRepository(ClinicalDbContext db, EfPatientRepository patients) : ITherapySessionRepository
{
    private IQueryable<TherapySession> Accessible()
    {
        var patientIds = patients.Accessible().Select(p => p.Id);
        return db.Sessions.AsNoTracking().Where(s => patientIds.Contains(s.PatientId));
    }

    public async Task<IReadOnlyList<TherapySession>> GetAllAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await Accessible().OrderByDescending(s => s.StartedAtUtc).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TherapySession>> GetByPatientIdAsync(Guid patientId, CancellationToken ct)
    {
        await patients.GetByIdAsync(patientId, ct); // 404 for patients outside the caseload
        return await db.Sessions.AsNoTracking().Where(s => s.PatientId == patientId)
            .OrderByDescending(s => s.StartedAtUtc).ToListAsync(ct);
    }

    public async Task<int> CountSinceAsync(DateTimeOffset sinceUtc, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        return await Accessible().CountAsync(s => s.StartedAtUtc >= sinceUtc && s.StartedAtUtc <= now, ct);
    }

    public async Task<int> CountByPlatformSinceAsync(TherapyPlatform platform, DateTimeOffset sinceUtc, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        return await Accessible().CountAsync(s => s.Platform == platform
            && s.StartedAtUtc >= sinceUtc && s.StartedAtUtc <= now, ct);
    }
}
