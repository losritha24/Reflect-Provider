using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Services;

public class InMemoryPatientRepository : IPatientRepository
{
    private readonly ICurrentUserService _currentUser;

    public InMemoryPatientRepository(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public Task<PatientPage> SearchAsync(string search, Reflect.ProviderDashboard.Domain.Enums.EngagementTrend? trend, int page, int pageSize, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = AccessiblePatients().Where(p => !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.DisplayName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)
                || p.ExternalPatientId.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        if (trend.HasValue) query = query.Where(p => p.Trend == trend.Value);
        var count = query.Count();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(count / (double)pageSize)));
        var items = query.OrderBy(p => p.DisplayName).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PatientPage(items, count, page, pageSize));
    }

    public Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var list = AccessiblePatients()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.DisplayName)
            .ToList();
        return Task.FromResult<IReadOnlyList<Patient>>(list);
    }

    public Task<Patient> GetByIdAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var patient = AccessiblePatients().FirstOrDefault(p => p.Id == id && !p.IsDeleted);
        if (patient == null)
        {
            throw new EntityNotFoundException("Patient not found.");
        }

        return Task.FromResult(patient);
    }

    public Task<Patient> AddAsync(string displayName, string externalPatientId, DateOnly dateOfBirth, string notesSummary, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var providerId = _currentUser.IsAuthenticated && _currentUser.IsInRole(ProviderAuthorization.ProviderRole)
            ? _currentUser.UserId?.ToString() : null;
        if (string.IsNullOrWhiteSpace(providerId))
            throw new InvalidOperationException("A signed-in provider is required to add a patient.");

        var name = displayName.Trim();
        var externalId = externalPatientId.Trim();
        if (AccessiblePatients().Any(patient =>
                string.Equals(patient.ExternalPatientId, externalId, StringComparison.OrdinalIgnoreCase)))
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
        InMemoryDataStore.AddPatient(patient);
        return Task.FromResult(patient);
    }

    public Task RemoveAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var patient = AccessiblePatients().FirstOrDefault(item => item.Id == id && !item.IsDeleted)
            ?? throw new EntityNotFoundException("Patient not found.");
        patient.IsDeleted = true;
        patient.DateDeleted = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    private IEnumerable<Patient> AccessiblePatients()
    {
        var providerId = _currentUser.IsAuthenticated && _currentUser.IsInRole(ProviderAuthorization.ProviderRole)
            ? _currentUser.UserId?.ToString() : null;
        return string.IsNullOrWhiteSpace(providerId)
            ? Enumerable.Empty<Patient>()
            : InMemoryDataStore.Patients.Where(p => p.AssignedProviderId == providerId);
    }
}
