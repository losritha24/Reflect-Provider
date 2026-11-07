using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Services;
using Reflect.ProviderDashboard.Web.Data;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.Identity;

namespace Reflect.ProviderDashboard.Web.Services;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";
    /// <summary>Daily email run. Off by default; a test notification can still be sent by hand.</summary>
    public bool Enabled { get; set; }
    public int SendHourLocal { get; set; } = 8;
    public SmtpOptions Smtp { get; set; } = new();
    public sealed class SmtpOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string User { get; set; } = "";
        /// <summary>Supplied only through secrets or host environment variables.</summary>
        public string Password { get; set; } = "";
        public string From { get; set; } = "";
        public bool EnableSsl { get; set; } = true;
        public bool Configured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
    }
}

public interface INotificationSender
{
    /// <summary>Returns true when the message was handed to a real mail server.</summary>
    Task<bool> SendAsync(string toEmail, string subject, string body, CancellationToken ct);
}

/// <summary>Default when SMTP is not configured: nothing leaves the app and the body is not kept.</summary>
public sealed class LogOnlyNotificationSender : INotificationSender
{
    public Task<bool> SendAsync(string toEmail, string subject, string body, CancellationToken ct) => Task.FromResult(false);
}

public sealed class SmtpNotificationSender(IOptions<NotificationOptions> options) : INotificationSender
{
    public async Task<bool> SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        var smtp = options.Value.Smtp;
        if (!smtp.Configured) return false;
        using var client = new SmtpClient(smtp.Host, smtp.Port) { EnableSsl = smtp.EnableSsl, Timeout = 15_000 };
        if (!string.IsNullOrWhiteSpace(smtp.User)) client.Credentials = new NetworkCredential(smtp.User, smtp.Password);
        using var message = new MailMessage(smtp.From, toEmail, subject, body);
        await client.SendMailAsync(message, ct);
        return true;
    }
}

public sealed record NotificationSummary(int Quieter, int BelowBaseline, int FollowUpsDue)
{
    public int Total => Quieter + BelowBaseline + FollowUpsDue;
}

/// <summary>
/// Builds the generic "you have items to review" message. Email is not a secure channel, so the body carries
/// no patient names, scores, modules or categories: only a count and a prompt to sign in.
/// </summary>
public sealed class NotificationService(ClinicalDbContext db, TimeProvider clock)
{
    public async Task<NotificationSummary> SummarizeAsync(string providerId, ProviderPreferences prefs, CancellationToken ct)
    {
        var patients = await db.Patients.AsNoTracking().Where(p => p.AssignedProviderId == providerId).ToListAsync(ct);
        var ids = patients.Select(p => p.Id).ToList();
        var now = clock.GetUtcNow();
        var since = now.AddDays(-7);
        var sessions = await db.Sessions.AsNoTracking().Where(s => ids.Contains(s.PatientId)).ToListAsync(ct);
        int quieter = 0, below = 0;
        foreach (var group in sessions.GroupBy(s => s.PatientId))
        {
            var current = group.Count(s => s.StartedAtUtc >= since && s.StartedAtUtc <= now);
            var previous = group.Count(s => s.StartedAtUtc >= since.AddDays(-7) && s.StartedAtUtc < since);
            if (current < previous) quieter++;
            if (EmotionalTrajectoryCalculator.Build(group.Where(s => s.StartedAtUtc <= now).ToList()).LatestDeviatesFromSelf) below++;
        }
        var dueSoon = (await db.FollowUps.AsNoTracking()
            .Where(f => f.Status == FollowUpStatus.Open && ids.Contains(f.PatientId)).ToListAsync(ct))
            .Count(f => f.DueAtUtc is { } d && d <= now.AddDays(1));
        return new NotificationSummary(prefs.NotifyQuieter ? quieter : 0, prefs.NotifyBaseline ? below : 0, dueSoon);
    }

    public static (string Subject, string Body) Compose(NotificationSummary summary) =>
        summary.Total == 0
            ? ("Reflect Provider: nothing to review", "Nothing needs your review today. Sign in to Reflect Provider any time to see your caseload.")
            : ($"Reflect Provider: {summary.Total} item{(summary.Total == 1 ? "" : "s")} to review",
               $"You have {summary.Total} item{(summary.Total == 1 ? "" : "s")} to review. Sign in to Reflect Provider to see details. This message does not include patient information.");

    /// <summary>True during the provider's quiet hours, including windows that cross midnight.</summary>
    public static bool IsQuiet(ProviderPreferences prefs, TimeOnly localNow)
    {
        if (!TimeOnly.TryParseExact(prefs.QuietFrom, "HH:mm", out var from) || !TimeOnly.TryParseExact(prefs.QuietTo, "HH:mm", out var to) || from == to)
            return false;
        return from < to ? localNow >= from && localNow < to : localNow >= from || localNow < to;
    }
}

/// <summary>Sends each opted-in provider at most one generic email a day, after the configured local hour and outside quiet hours.</summary>
public sealed class NotificationWorker(IServiceScopeFactory scopes, IOptions<NotificationOptions> options, ICompanyClock company, TimeProvider clock, ILogger<NotificationWorker> log) : BackgroundService
{
    private readonly Dictionary<string, DateOnly> lastSent = [];

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        if (!options.Value.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stop))
        {
            try { await RunOnceAsync(stop); }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning("Notification run failed: {Type}", ex.GetType().Name); }
        }
    }

    internal async Task RunOnceAsync(CancellationToken ct)
    {
        var local = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(company.IanaId));
        if (local.Hour < options.Value.SendHourLocal) return;
        var today = DateOnly.FromDateTime(local.DateTime);
        await using var scope = scopes.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ProviderUser>>();
        var service = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();
        var audit = scope.ServiceProvider.GetRequiredService<AuditLog>();
        foreach (var user in await users.GetUsersInRoleAsync(ProviderAuthorization.ProviderRole))
        {
            if (string.IsNullOrWhiteSpace(user.Email) || (lastSent.TryGetValue(user.Id, out var day) && day == today)) continue;
            var prefs = ProviderPreferences.From(await users.GetClaimsAsync(user));
            if (prefs.Channel != "email" || NotificationService.IsQuiet(prefs, TimeOnly.FromDateTime(local.DateTime))) continue;
            var summary = await service.SummarizeAsync(user.Id, prefs, ct);
            lastSent[user.Id] = today;
            if (summary.Total == 0) continue;
            var (subject, body) = NotificationService.Compose(summary);
            var delivered = await sender.SendAsync(user.Email, subject, body, ct);
            await audit.RecordAsync("system", delivered ? "notification-emailed" : "notification-not-sent", user.Id, ct);
        }
    }
}
