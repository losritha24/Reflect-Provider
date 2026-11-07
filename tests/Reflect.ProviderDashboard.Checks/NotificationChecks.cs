using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Web.Data;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Checks;

public static class NotificationChecks
{
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        Console.WriteLine($"PASS: {description}");
    }

    public static async Task RunAsync()
    {
        var ct = CancellationToken.None;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ClinicalDbContext>().UseSqlite(connection).Options;
        InMemoryDataStore.Seed();
        await using var db = new ClinicalDbContext(options);
        await ClinicalSeeder.EnsureAsync(db, "provider-1", reseed: true);

        var service = new NotificationService(db, TimeProvider.System);
        var all = await service.SummarizeAsync("provider-1", new ProviderPreferences(), ct);
        Check(all.Quieter >= 1 && all.BelowBaseline >= 1 && all.Total == all.Quieter + all.BelowBaseline + all.FollowUpsDue,
            "The summary counts quieter and below-baseline patients from the seeded data");
        var muted = await service.SummarizeAsync("provider-1", new ProviderPreferences { NotifyQuieter = false, NotifyBaseline = false }, ct);
        Check(muted.Quieter == 0 && muted.BelowBaseline == 0, "Turned-off categories are not counted");
        Check((await service.SummarizeAsync("someone-else", new ProviderPreferences(), ct)).Total == 0, "A provider with no patients gets nothing");

        var (subject, body) = NotificationService.Compose(all);
        var names = InMemoryDataStore.Patients.Select(p => p.DisplayName.Split(' ')[0]);
        Check(!names.Any(n => subject.Contains(n) || body.Contains(n)) && body.Contains("does not include patient information"),
            "The email carries a count only, never a patient name");
        Check(NotificationService.Compose(new NotificationSummary(0, 0, 0)).Subject.Contains("nothing"), "An empty summary says there is nothing to review");

        var overnight = new ProviderPreferences { QuietFrom = "22:00", QuietTo = "07:00" };
        Check(NotificationService.IsQuiet(overnight, new TimeOnly(23, 30)) && NotificationService.IsQuiet(overnight, new TimeOnly(6, 59))
            && !NotificationService.IsQuiet(overnight, new TimeOnly(7, 0)) && !NotificationService.IsQuiet(overnight, new TimeOnly(12, 0)),
            "Quiet hours that cross midnight are honoured");
        Check(!NotificationService.IsQuiet(new ProviderPreferences(), new TimeOnly(3, 0)), "No quiet hours means never quiet");
        Check(!await new LogOnlyNotificationSender().SendAsync("a@example.invalid", "s", "b", ct), "Without SMTP nothing is reported as delivered");
        Check(!await new SmtpNotificationSender(Microsoft.Extensions.Options.Options.Create(new NotificationOptions())).SendAsync("a@example.invalid", "s", "b", ct),
            "An unconfigured SMTP sender sends nothing");
    }
}
