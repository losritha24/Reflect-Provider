using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Data;

namespace Reflect.ProviderDashboard.Web.Services;

public class EfPatientRepository(ClinicalDbContext db, ICurrentUserService currentUser) : IPatientRepository
{
    // Read-only queries; nothing here mutates caller-visible state.
    internal IQueryable<Patient> Accessible()
    {
        var providerId = ProviderScope.CurrentProviderId(currentUser);
        return string.IsNullOrWhiteSpace(providerId)
            ? db.Patients.Where(_ => false)
            : db.Patients.AsNoTracking().Where(p => p.AssignedProviderId == providerId);
    }

    public async Task<PatientPage> SearchAsync(string search, EngagementTrend? trend, int page, int pageSize, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = Accessible();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.DisplayName.ToLower().Contains(term) || p.ExternalPatientId.ToLower().Contains(term));
        }
        if (trend.HasValue) query = query.Where(p => p.Trend == trend.Value);
        var count = await query.CountAsync(ct);
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(count / (double)pageSize)));
        var items = await query.OrderBy(p => p.DisplayName).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PatientPage(items, count, page, pageSize);
    }

    public async Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await Accessible().OrderBy(p => p.DisplayName).ThenBy(p => p.Id).ToListAsync(ct);
    }

    public async Task<Patient> GetByIdAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await Accessible().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new EntityNotFoundException("Patient not found.");
    }
}
