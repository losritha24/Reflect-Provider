using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Reflect.ProviderDashboard.Web.Data;

namespace Reflect.ProviderDashboard.Web.Infrastructure;

public sealed class AccountStoreHealthCheck(IServiceScopeFactory scopes) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ProviderDbContext>();
            // A schema query also detects an empty/missing store; connectivity alone does not.
            await database.Users.AnyAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            // Public health results must not contain paths, SQL, or account details.
            return HealthCheckResult.Unhealthy("Account store unavailable.");
        }
    }
}
