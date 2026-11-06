using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Web.Data;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Checks;

/// <summary>Runs the database-backed repositories against a real (in-memory) SQLite engine.</summary>
public static class ClinicalStoreChecks
{
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        Console.WriteLine($"PASS: {description}");
    }

    public static async Task RunAsync()
    {
        var ct = CancellationToken.None;
        var owner = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ClinicalDbContext>().UseSqlite(connection).Options;

        InMemoryDataStore.Seed();
        await using var db = new ClinicalDbContext(options);
        await ClinicalSeeder.EnsureAsync(db, owner.ToString(), reseed: true);
        await ClinicalSeeder.EnsureAsync(db, owner.ToString(), reseed: true);
        Check(await db.Patients.CountAsync() == 5 && await db.Sessions.CountAsync() == 12, "Reseeding replaces rows instead of duplicating them");

        var mine = new EfPatientRepository(db, new TestCurrentUserService(owner));
        var sessions = new EfTherapySessionRepository(db, mine);
        var all = await mine.GetAllAsync(ct);
        Check(all.Count == 5, "Assigned provider sees the whole caseload from the database");

        var first = await mine.SearchAsync(null, null, 1, 2, ct);
        var second = await mine.SearchAsync(null, null, 2, 2, ct);
        Check(first.Items.Count == 2 && !first.Items.Select(p => p.Id).Intersect(second.Items.Select(p => p.Id)).Any(), "DB pages do not overlap");
        Check((await mine.SearchAsync(null, null, int.MaxValue, 2, ct)).Page == 3, "DB out-of-range pages clamp to last page");
        var sam = await mine.SearchAsync("  sAM  ", EngagementTrend.Declining, 1, 10, ct);
        Check(sam.TotalCount == 1 && sam.Items[0].DisplayName == "Sam Patel", "DB search is case-insensitive and combines filters");
        Check((await mine.SearchAsync("PT-10052", null, 1, 10, ct)).TotalCount == 1, "DB external IDs are searchable");
        Check((await mine.SearchAsync("100%_", null, 1, 10, ct)).TotalCount == 0, "LIKE wildcards in search are treated as text");

        var other = new EfPatientRepository(db, new TestCurrentUserService(Guid.NewGuid()));
        var otherSessions = new EfTherapySessionRepository(db, other);
        Check((await other.GetAllAsync(ct)).Count == 0 && (await otherSessions.GetAllAsync(ct)).Count == 0, "Another provider sees no patients or sessions");
        try { await other.GetByIdAsync(all[0].Id, ct); throw new Exception("Cross-provider read succeeded"); }
        catch (EntityNotFoundException) { Console.WriteLine("PASS: Cross-provider patient read is not found"); }
        try { await otherSessions.GetByPatientIdAsync(all[0].Id, ct); throw new Exception("Cross-provider sessions read succeeded"); }
        catch (EntityNotFoundException) { Console.WriteLine("PASS: Cross-provider session read is not found"); }
        var anonymous = new EfPatientRepository(db, new TestCurrentUserService(owner, authenticated: false));
        var noRole = new EfPatientRepository(db, new TestCurrentUserService(owner, provider: false));
        Check((await anonymous.GetAllAsync(ct)).Count == 0 && (await noRole.GetAllAsync(ct)).Count == 0, "Unauthenticated or role-less identities fail closed");

        var list = await sessions.GetAllAsync(ct);
        Check(list.Count == 12 && list.Zip(list.Skip(1), (a, b) => a.StartedAtUtc >= b.StartedAtUtc).All(x => x), "Sessions sort newest-first in the database");
        Check((await sessions.GetByPatientIdAsync(all[0].Id, ct)).All(s => s.PatientId == all[0].Id), "Patient sessions are scoped to the patient");
        Check(await sessions.CountSinceAsync(DateTimeOffset.UtcNow.AddDays(-7), ct) == 8
            && await sessions.CountByPlatformSinceAsync(TherapyPlatform.Vr, DateTimeOffset.UtcNow.AddDays(-7), ct) == 4,
            "Seven-day totals match the seeded data");

        try { await mine.SearchAsync(null, null, 1, 10, new CancellationToken(true)); throw new Exception("Cancellation was ignored"); }
        catch (OperationCanceledException) { Console.WriteLine("PASS: DB search honors cancellation"); }

        var gone = await db.Patients.FirstAsync(p => p.Id == all[0].Id);
        gone.IsDeleted = true; await db.SaveChangesAsync();
        Check((await mine.GetAllAsync(ct)).Count == 4, "Soft-deleted patients are hidden");
    }
}
