using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Data;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Checks;

public static class FollowUpChecks
{
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        Console.WriteLine($"PASS: {description}");
    }

    private sealed class Fake(Guid id) : ICurrentUserService
    {
        public Guid? UserId { get; } = id;
        public string EntraObjectId => "";
        public string Email => "";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => true;
    }

    private static async Task Throws<T>(Func<Task> action, string description) where T : Exception
    {
        try { await action(); }
        catch (T) { Console.WriteLine($"PASS: {description}"); return; }
        throw new Exception(description + " (no exception)");
    }

    public static async Task RunAsync()
    {
        var ct = CancellationToken.None;
        var file = Path.Combine(Path.GetTempPath(), $"followups-{Guid.NewGuid():N}.db");
        try
        {
            var a = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000a");
            var b = Guid.Parse("bbbbbbbb-0000-0000-0000-00000000000b");
            DbContextOptions<ClinicalDbContext> Options() => new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseSqlite(new SqliteConnectionStringBuilder { DataSource = file }.ToString()).Options;

            Guid patientId, taskId;
            await using (var db = new ClinicalDbContext(Options()))
            {
                InMemoryDataStore.Seed();
                await ClinicalSeeder.EnsureAsync(db, a.ToString(), reseed: true);
                var asA = new FollowUpService(db, new EfPatientRepository(db, new Fake(a)), new Fake(a), TimeProvider.System);
                var asB = new FollowUpService(db, new EfPatientRepository(db, new Fake(b)), new Fake(b), TimeProvider.System);
                patientId = (await new EfPatientRepository(db, new Fake(a)).GetAllAsync(ct))[0].Id;

                var task = await asA.CreateAsync(patientId, "  Check in about sleep  ", "Brief note", DateTimeOffset.UtcNow.AddDays(-1), ct);
                taskId = task.Id;
                Check(task.Title == "Check in about sleep" && task.Version == 1 && task.Status == FollowUpStatus.Open, "A follow-up is created trimmed, open, at version 1");
                Check((await asA.HistoryAsync(taskId, ct)).Count == 1, "Creating writes a history event in the same save");
                Check((await asA.DueAsync(7, ct)).Single().Overdue, "An overdue open follow-up appears in the due list");

                await Throws<EntityNotFoundException>(() => asB.ListAsync(patientId, ct), "Another provider cannot list follow-ups on this patient");
                await Throws<EntityNotFoundException>(() => asB.CreateAsync(patientId, "x", "", null, ct), "Another provider cannot create a follow-up on this patient");
                await Throws<EntityNotFoundException>(() => asB.SetStatusAsync(taskId, FollowUpStatus.Completed, 1, ct), "Another provider cannot complete this follow-up");
                await Throws<EntityNotFoundException>(() => asB.HistoryAsync(taskId, ct), "Another provider cannot read this follow-up's history");
                Check((await asB.DueAsync(30, ct)).Count == 0 && await asB.OpenCountAsync(ct) == 0, "Another provider's due list and counts stay empty");

                await Throws<ArgumentException>(() => asA.CreateAsync(patientId, "   ", "", null, ct), "A blank title is rejected");
                await Throws<ArgumentException>(() => asA.CreateAsync(patientId, new string('t', FollowUpService.MaxTitle + 1), "", null, ct), "An over-long title is rejected");
                await Throws<ArgumentException>(() => asA.CreateAsync(patientId, "ok", new string('n', FollowUpService.MaxNote + 1), null, ct), "An over-long note is rejected");
                await Throws<ArgumentException>(() => asA.CreateAsync(patientId, "ok", "", DateTimeOffset.UtcNow.AddYears(5), ct), "A due date far in the future is rejected");
                await Throws<EntityNotFoundException>(() => asA.CreateAsync(Guid.NewGuid(), "ok", "", null, ct), "A missing patient is a 404");

                var done = await asA.SetStatusAsync(taskId, FollowUpStatus.Completed, 1, ct);
                Check(done.Version == 2 && done.Status == FollowUpStatus.Completed && (await asA.HistoryAsync(taskId, ct)).Count == 2, "Completing bumps the version and appends an event");
                await Throws<StaleVersionException>(() => asA.SetStatusAsync(taskId, FollowUpStatus.Dismissed, 1, ct), "A duplicate or stale submit is refused");
                var reopened = await asA.SetStatusAsync(taskId, FollowUpStatus.Open, 2, ct);
                Check(reopened.Status == FollowUpStatus.Open && reopened.Version == 3, "A completed follow-up can be reopened with the current version");
                Check((await asA.HistoryAsync(taskId, ct)).Select(e => e.Action).SequenceEqual(["created", "completed", "reopened"]), "History is ordered and append-only");

                // A concurrent writer wins: our save must fail rather than overwrite.
                var racing = await db.FollowUps.FirstAsync(f => f.Id == taskId, ct);
                await using (var other = new ClinicalDbContext(Options()))
                    await other.FollowUps.Where(f => f.Id == taskId).ExecuteUpdateAsync(s => s.SetProperty(f => f.Version, 99), ct);
                racing.Status = FollowUpStatus.Dismissed;
                racing.Version++;
                await Throws<DbUpdateConcurrencyException>(() => db.SaveChangesAsync(ct), "A concurrent change makes the save fail instead of overwriting");
                db.ChangeTracker.Clear();
                await db.FollowUps.Where(f => f.Id == taskId).ExecuteUpdateAsync(s => s.SetProperty(f => f.Version, 3), ct);

                var patient = await db.Patients.FirstAsync(p => p.Id == patientId, ct);
                patient.IsDeleted = true;
                await db.SaveChangesAsync(ct);
                await Throws<EntityNotFoundException>(() => asA.ListAsync(patientId, ct), "A deleted patient's follow-ups are a 404");
                patient.IsDeleted = false;
                await db.SaveChangesAsync(ct);
            }

            await using (var reopen = new ClinicalDbContext(Options()))
            {
                var again = new FollowUpService(reopen, new EfPatientRepository(reopen, new Fake(a)), new Fake(a), TimeProvider.System);
                Check((await again.ListAsync(patientId, ct)).Any(f => f.Id == taskId) && (await again.HistoryAsync(taskId, ct)).Count == 3, "Follow-ups and history persist across a restart");
            }
        }
        finally { SqliteConnection.ClearAllPools(); File.Delete(file); }
    }
}
