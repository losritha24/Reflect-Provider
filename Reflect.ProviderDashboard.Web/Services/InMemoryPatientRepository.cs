using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Services;

public class InMemoryPatientRepository : IPatientRepository
{
    public Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken ct)
    {
        var list = InMemoryDataStore.Patients
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.DisplayName)
            .ToList();
        return Task.FromResult<IReadOnlyList<Patient>>(list);
    }

    public Task<Patient> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var patient = InMemoryDataStore.Patients.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
        if (patient == null)
        {
            throw new EntityNotFoundException("Patient not found.");
        }

        return Task.FromResult(patient);
    }
}
