using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Data;

public static class ClinicalSeeder
{
    /// <summary>
    /// Loads the synthetic dataset. <paramref name="reseed"/> is only for the disposable demo:
    /// it replaces all rows so relative dates ("last 7 days") stay fresh. Never set it where real data lives.
    /// </summary>
    public static async Task EnsureAsync(ClinicalDbContext db, string providerId, bool reseed, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);
        if (reseed)
        {
            await db.Sessions.IgnoreQueryFilters().ExecuteDeleteAsync(ct);
            await db.Patients.IgnoreQueryFilters().ExecuteDeleteAsync(ct);
        }
        else if (await db.Patients.IgnoreQueryFilters().AnyAsync(ct))
        {
            return;
        }

        foreach (var patient in InMemoryDataStore.Patients)
            patient.AssignedProviderId = providerId ?? string.Empty;
        db.Patients.AddRange(InMemoryDataStore.Patients);
        db.Sessions.AddRange(InMemoryDataStore.Sessions);
        await db.SaveChangesAsync(ct);
    }
}
