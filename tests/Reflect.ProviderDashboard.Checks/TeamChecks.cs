using System.Security.Claims;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Web.Data;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Checks;

/// <summary>Invites, facility assignment and account preferences.</summary>
public static class TeamChecks
{
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        Console.WriteLine($"PASS: {description}");
    }

    private sealed class ManualClock(DateTimeOffset start) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = start;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    public static async Task RunAsync()
    {
        var clock = new ManualClock(new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero));
        var team = new TeamStore(clock);

        var token = team.CreateInvite("new.provider@example.invalid", "New Provider", "admin@example.invalid", out var invite);
        Check(invite.TokenHash != token && !invite.TokenHash.Contains(token), "Invite tokens are stored only as hashes");
        Check(team.FindUsable(token)?.Id == invite.Id, "A fresh invite token resolves to its invite");
        Check(team.FindUsable(token + "x") is null && team.FindUsable("") is null && team.FindUsable(new string('a', 500)) is null,
            "Wrong, empty and oversized tokens are rejected");

        Check(team.MarkAccepted(invite.Id) && team.FindUsable(token) is null, "An accepted invite cannot be reused");
        Check(!team.MarkAccepted(invite.Id), "An invite cannot be accepted twice");

        var second = team.CreateInvite("second@example.invalid", "Second", "admin@example.invalid", out var secondInvite);
        var replaced = team.CreateInvite("SECOND@example.invalid", "Second", "admin@example.invalid", out _);
        Check(team.FindUsable(second) is null && team.FindUsable(replaced) is not null, "A newer invite for the same email supersedes the older one");
        Check(team.Revoke(team.FindUsable(replaced)!.Id) && team.FindUsable(replaced) is null, "A revoked invite stops working");

        var expiring = team.CreateInvite("late@example.invalid", "Late", "admin@example.invalid", out _);
        clock.Now = clock.Now + TeamStore.InviteLifetime + TimeSpan.FromSeconds(1);
        Check(team.FindUsable(expiring) is null, "Invites expire after 72 hours");

        var sent = team.Queue("provider-1", Guid.NewGuid(), "Sam", "hello");
        Check(team.Outbox("provider-1").Count == 1 && team.Outbox("provider-2").Count == 0 && sent.Status.Contains("not delivered"),
            "Messages are queued per provider and labelled as not delivered");

        await CheckAuditAsync(clock);
        await CheckAssignmentAsync();
        CheckPreferences();
    }

    private static async Task CheckAuditAsync(TimeProvider clock)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ProviderDbContext>().UseSqlite(connection).Options;
        await using var db = new ProviderDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var audit = new AuditLog(db, clock);
        await audit.RecordAsync("a@example.invalid", "x", "one");
        await audit.RecordAsync("b@example.invalid", "y", "two");
        await audit.RecordAsync("a@example.invalid", "z", new string('d', 900));
        var mine = await audit.QueryAsync("a@example.invalid");
        Check(mine.Count == 2 && (await audit.QueryAsync()).Count == 3 && mine[0].Detail.Length == 500,
            "Audit entries are durable, filterable by actor, and bounded in length");
        Check(await audit.VerifyAsync() is null, "An untouched audit chain verifies");
        await db.Database.ExecuteSqlRawAsync("UPDATE \"AuditEvents\" SET \"Detail\" = 'edited' WHERE \"Action\" = 'y'");
        Check(await audit.VerifyAsync() == 2, "Editing an audit row is detected as the first broken link");

        // Existing databases get new tables without touching old ones.
        await using var oldDb = new SqliteConnection("Data Source=:memory:");
        await oldDb.OpenAsync();
        var oldOptions = new DbContextOptionsBuilder<ProviderDbContext>().UseSqlite(oldDb).Options;
        await using var legacy = new ProviderDbContext(oldOptions);
        await legacy.Database.EnsureCreatedAsync();
        await legacy.Database.ExecuteSqlRawAsync("DROP TABLE \"AuditEvents\"");
        await SchemaUpgrade.EnsureTablesAsync(legacy, CancellationToken.None, "AuditEvents");
        await new AuditLog(legacy, clock).RecordAsync("c@example.invalid", "q", "after upgrade");
        Check((await new AuditLog(legacy, clock).QueryAsync()).Count == 1, "A missing table is created on an existing database");
    }

    private static async Task CheckAssignmentAsync()
    {
        var ct = CancellationToken.None;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ClinicalDbContext>().UseSqlite(connection).Options;
        InMemoryDataStore.Seed();
        await using var db = new ClinicalDbContext(options);
        var providerA = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000a");
        var providerB = Guid.Parse("bbbbbbbb-0000-0000-0000-00000000000b");
        await ClinicalSeeder.EnsureAsync(db, providerA.ToString(), reseed: true);

        var service = new PatientAssignmentService(db);
        var all = await service.ListAsync(ct);
        var counts = await service.CountsAsync(ct);
        Check(all.Count == InMemoryDataStore.Patients.Count && counts[providerA.ToString()] == all.Count,
            "Admin listing and per-provider counts reflect the seeded assignment");

        var moved = all[0];
        var previous = await service.AssignAsync(moved.Id, providerB.ToString(), ct);
        Check(previous == providerA.ToString(), "Assigning reports the previous provider");

        var asA = new EfPatientRepository(db, new Fake(providerA));
        var asB = new EfPatientRepository(db, new Fake(providerB));
        Check((await asB.GetAllAsync(ct)).Select(p => p.Id).SequenceEqual([moved.Id]),
            "The newly assigned provider sees exactly the patient moved to them");
        Check((await asA.GetAllAsync(ct)).All(p => p.Id != moved.Id), "The previous provider loses access after reassignment");
        var sessions = new EfTherapySessionRepository(db, asB);
        Check((await sessions.GetByPatientIdAsync(moved.Id, ct)).Count > 0, "Past sessions travel with the reassigned patient");

        await service.AssignAsync(moved.Id, "", ct);
        Check((await asB.GetAllAsync(ct)).Count == 0 && (await service.ListAsync(ct)).First(p => p.Id == moved.Id).AssignedProviderId == "",
            "Unassigning removes the patient from every provider");
        try { await service.AssignAsync(Guid.NewGuid(), providerB.ToString(), ct); throw new Exception("Unknown patient assignment was allowed"); }
        catch (Reflect.ProviderDashboard.Domain.Exceptions.EntityNotFoundException) { Console.WriteLine("PASS: Assigning an unknown patient fails"); }
    }

    private static void CheckPreferences()
    {
        var prefs = new ProviderPreferences
        {
            Theme = "dark", TextSize = "125%", LandingPage = "Patients", ShowTour = true, NotifyQuieter = false,
            Channel = "push", QuietFrom = "22:00", QuietTo = "07:30", Title = "  LCSW  "
        };
        var round = ProviderPreferences.From(prefs.ToClaims());
        Check(round.Theme == "dark" && round.TextSize == "125%" && round.LandingPage == "Patients" && round.ShowTour
            && !round.NotifyQuieter && round.NotifyBaseline && round.Channel == "push"
            && round.QuietFrom == "22:00" && round.QuietTo == "07:30" && round.Title == "LCSW",
            "Preferences survive a round trip through claims");

        var hostile = new ProviderPreferences { Theme = "<script>", TextSize = "9999%", LandingPage = "../admin", Channel = "sms", QuietFrom = "25:99", Clock = "36h" };
        var clean = ProviderPreferences.From(hostile.ToClaims());
        Check(clean.Theme == "" && clean.TextSize == "" && clean.LandingPage == "Dashboard" && clean.Channel == "in-app"
            && clean.QuietFrom == "" && clean.Clock == "12h",
            "Unknown or hostile preference values fall back to safe defaults");
        Check(ProviderPreferences.From([new Claim("other", "x")]).LandingPage == "Dashboard", "Missing preferences use defaults");
    }

    private sealed class Fake(Guid id) : Reflect.ProviderDashboard.Domain.Interfaces.ICurrentUserService
    {
        public Guid? UserId { get; } = id;
        public string EntraObjectId => "";
        public string Email => "";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => true;
    }
}
