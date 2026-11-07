using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Web.Data;

namespace Reflect.ProviderDashboard.Web.Services;

public sealed record AssignablePatient(Guid Id, string ExternalPatientId, string AssignedProviderId);

/// <summary>
/// Facility-level assignment. Deliberately exposes only the external ID and current provider,
/// not names or session data: administrators move patients, they do not read charts.
/// Callers must already be authorized as a facility admin.
/// </summary>
public sealed class PatientAssignmentService(ClinicalDbContext db)
{
    public async Task<IReadOnlyList<AssignablePatient>> ListAsync(CancellationToken ct) =>
        await db.Patients.AsNoTracking().OrderBy(p => p.ExternalPatientId)
            .Select(p => new AssignablePatient(p.Id, p.ExternalPatientId, p.AssignedProviderId)).ToListAsync(ct);

    public async Task<IReadOnlyDictionary<string, int>> CountsAsync(CancellationToken ct) =>
        await db.Patients.AsNoTracking().Where(p => p.AssignedProviderId != "")
            .GroupBy(p => p.AssignedProviderId).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

    /// <summary>Assigns to a provider id, or unassigns when providerId is empty. Past sessions are untouched.</summary>
    public async Task<string> AssignAsync(Guid patientId, string providerId, CancellationToken ct)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == patientId, ct)
            ?? throw new EntityNotFoundException("Patient not found.");
        var previous = patient.AssignedProviderId;
        patient.AssignedProviderId = providerId ?? string.Empty;
        await db.SaveChangesAsync(ct);
        return previous;
    }
}
