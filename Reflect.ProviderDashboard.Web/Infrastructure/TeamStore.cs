using System.Security.Cryptography;
using System.Text;

namespace Reflect.ProviderDashboard.Web.Infrastructure;

public enum InviteStatus { Pending, Accepted, Revoked }

public sealed record Invite(
    Guid Id, string Email, string DisplayName, string TokenHash,
    DateTimeOffset CreatedUtc, DateTimeOffset ExpiresUtc, string CreatedBy, InviteStatus Status);

public sealed record AuditEntry(DateTimeOffset AtUtc, string Actor, string Action, string Detail);

public sealed record OutboxMessage(
    Guid Id, string ProviderId, Guid PatientId, string PatientLabel, string Body,
    DateTimeOffset CreatedUtc, string Status);

/// <summary>
/// Single-instance prototype store for invites, the audit trail and the patient message outbox.
/// Resets on restart. Replace with tables in the account/clinical databases (see docs/TODO.md).
/// Invite tokens are stored only as SHA-256 hashes; the raw token is shown once to the admin.
/// </summary>
public sealed class TeamStore(TimeProvider clock)
{
    public static readonly TimeSpan InviteLifetime = TimeSpan.FromHours(72);
    private const int MaxAudit = 500;

    private readonly object gate = new();
    private readonly List<Invite> invites = [];
    private readonly List<AuditEntry> audit = [];
    private readonly List<OutboxMessage> outbox = [];

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>Creates an invite and returns the one-time raw token.</summary>
    public string CreateInvite(string email, string displayName, string createdBy, out Invite invite)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var now = clock.GetUtcNow();
        invite = new Invite(Guid.NewGuid(), email.Trim(), displayName.Trim(), Hash(token), now, now + InviteLifetime, createdBy, InviteStatus.Pending);
        lock (gate)
        {
            // A new invite for the same email supersedes any pending one.
            for (var i = 0; i < invites.Count; i++)
                if (invites[i].Status == InviteStatus.Pending && invites[i].Email.Equals(invite.Email, StringComparison.OrdinalIgnoreCase))
                    invites[i] = invites[i] with { Status = InviteStatus.Revoked };
            invites.Add(invite);
        }
        return token;
    }

    public IReadOnlyList<Invite> Invites()
    {
        lock (gate) return invites.OrderByDescending(i => i.CreatedUtc).ToList();
    }

    /// <summary>Returns the invite only while it is pending and unexpired.</summary>
    public Invite FindUsable(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 128) return null;
        var hash = Hash(token);
        lock (gate)
        {
            var invite = invites.FirstOrDefault(i => CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(i.TokenHash), Encoding.ASCII.GetBytes(hash)));
            return invite is { Status: InviteStatus.Pending } && invite.ExpiresUtc > clock.GetUtcNow() ? invite : null;
        }
    }

    public bool MarkAccepted(Guid id) => SetStatus(id, InviteStatus.Accepted);
    public bool Revoke(Guid id) => SetStatus(id, InviteStatus.Revoked);

    private bool SetStatus(Guid id, InviteStatus status)
    {
        lock (gate)
        {
            var index = invites.FindIndex(i => i.Id == id && i.Status == InviteStatus.Pending);
            if (index < 0) return false;
            invites[index] = invites[index] with { Status = status };
            return true;
        }
    }

    public void Record(string actor, string action, string detail)
    {
        lock (gate)
        {
            audit.Add(new AuditEntry(clock.GetUtcNow(), actor, action, detail));
            if (audit.Count > MaxAudit) audit.RemoveRange(0, audit.Count - MaxAudit);
        }
    }

    public IReadOnlyList<AuditEntry> Audit(Func<AuditEntry, bool> filter = null, int take = 50)
    {
        lock (gate) return audit.Where(filter ?? (_ => true)).OrderByDescending(a => a.AtUtc).Take(take).ToList();
    }

    public OutboxMessage Queue(string providerId, Guid patientId, string patientLabel, string body)
    {
        var message = new OutboxMessage(Guid.NewGuid(), providerId, patientId, patientLabel, body, clock.GetUtcNow(),
            "Queued (prototype, not delivered)");
        lock (gate) outbox.Add(message);
        return message;
    }

    public IReadOnlyList<OutboxMessage> Outbox(string providerId)
    {
        lock (gate) return outbox.Where(m => m.ProviderId == providerId).OrderByDescending(m => m.CreatedUtc).ToList();
    }
}
