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

    public async Task<Patient> AddAsync(string displayName, string externalPatientId, DateOnly dateOfBirth, string notesSummary, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var providerId = ProviderScope.CurrentProviderId(currentUser);
        if (string.IsNullOrWhiteSpace(providerId))
            throw new InvalidOperationException("A signed-in provider is required to add a patient.");

        var name = displayName.Trim();
        var externalId = externalPatientId.Trim();
        var duplicate = await db.Patients.IgnoreQueryFilters()
            .AnyAsync(patient =>
                patient.AssignedProviderId == providerId
                && patient.ExternalPatientId.ToLower() == externalId.ToLower(), ct);
        if (duplicate)
            throw new DuplicateExternalPatientIdException();

        var patient = new Patient
        {
            DisplayName = name,
            ExternalPatientId = externalId,
            DateOfBirth = dateOfBirth,
            NotesSummary = notesSummary?.Trim() ?? string.Empty,
            Trend = EngagementTrend.Stable,
            AssignedProviderId = providerId
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync(ct);
        return patient;
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var providerId = ProviderScope.CurrentProviderId(currentUser);
        if (string.IsNullOrWhiteSpace(providerId))
            throw new InvalidOperationException("A signed-in provider is required to remove a patient.");

        var patient = await db.Patients.FirstOrDefaultAsync(
                item => item.Id == id && item.AssignedProviderId == providerId, ct)
            ?? throw new EntityNotFoundException("Patient not found.");
        patient.IsDeleted = true;
        patient.DateDeleted = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
