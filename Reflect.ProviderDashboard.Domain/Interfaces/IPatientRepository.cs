using Reflect.ProviderDashboard.Domain.Entities;

namespace Reflect.ProviderDashboard.Domain.Interfaces;

public interface IPatientRepository
{
    Task<PatientPage> SearchAsync(string search, Reflect.ProviderDashboard.Domain.Enums.EngagementTrend? trend, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken ct);
    Task<Patient> GetByIdAsync(Guid id, CancellationToken ct);
}
