using Reflect.ProviderDashboard.Domain.Entities;

namespace Reflect.ProviderDashboard.Domain.Interfaces;

public interface IPatientRepository
{
    Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken ct);
    Task<Patient> GetByIdAsync(Guid id, CancellationToken ct);
}
