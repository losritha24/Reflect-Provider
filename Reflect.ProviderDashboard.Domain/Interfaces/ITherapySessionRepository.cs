using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;

namespace Reflect.ProviderDashboard.Domain.Interfaces;

public interface ITherapySessionRepository
{
    Task<IReadOnlyList<TherapySession>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<TherapySession>> GetByPatientIdAsync(Guid patientId, CancellationToken ct);
    Task<int> CountSinceAsync(DateTimeOffset sinceUtc, CancellationToken ct);
    Task<int> CountByPlatformSinceAsync(TherapyPlatform platform, DateTimeOffset sinceUtc, CancellationToken ct);
}
