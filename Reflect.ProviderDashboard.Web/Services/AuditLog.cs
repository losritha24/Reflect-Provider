using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Web.Data;

namespace Reflect.ProviderDashboard.Web.Services;

public sealed record AuditEntry(DateTimeOffset AtUtc, string Actor, string Action, string Detail);

public sealed class AuditEvent
{
    public long Seq { get; set; }
    public long AtTicks { get; set; }
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string Detail { get; set; } = "";
    public string PrevHash { get; set; } = "";
    public string Hash { get; set; } = "";
}

/// <summary>
/// Append-only, hash-chained audit trail stored in the account database, so it survives restarts and
/// is not wiped when the synthetic clinical data is reseeded. There is deliberately no update or delete.
/// Detail must never contain patient names, message text, tokens or passwords: use IDs only.
/// </summary>
public sealed class AuditLog(ProviderDbContext db, TimeProvider clock)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string Cut(string value, int max) => (value ?? "").Length > max ? value[..max] : value ?? "";

    public static string ComputeHash(string prev, long ticks, string actor, string action, string detail) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{prev}|{ticks}|{actor}|{action}|{detail}")));

    public async Task RecordAsync(string actor, string action, string detail, CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            var prev = await db.AuditEvents.AsNoTracking().OrderByDescending(e => e.Seq).Select(e => e.Hash).FirstOrDefaultAsync(ct) ?? "";
            var entry = new AuditEvent
            {
                AtTicks = clock.GetUtcNow().UtcTicks,
                Actor = Cut(actor, 256), Action = Cut(action, 64), Detail = Cut(detail, 500), PrevHash = prev
            };
            entry.Hash = ComputeHash(prev, entry.AtTicks, entry.Actor, entry.Action, entry.Detail);
            db.AuditEvents.Add(entry);
            await db.SaveChangesAsync(ct);
        }
        finally { Gate.Release(); }
    }

    public async Task<IReadOnlyList<AuditEntry>> QueryAsync(string actor = null, int take = 50, CancellationToken ct = default)
    {
        var query = db.AuditEvents.AsNoTracking();
        if (!string.IsNullOrEmpty(actor)) query = query.Where(e => e.Actor == actor);
        var rows = await query.OrderByDescending(e => e.Seq).Take(Math.Clamp(take, 1, 500)).ToListAsync(ct);
        return rows.Select(e => new AuditEntry(new DateTimeOffset(e.AtTicks, TimeSpan.Zero), e.Actor, e.Action, e.Detail)).ToList();
    }

    /// <summary>Returns the sequence number of the first broken link, or null when the chain is intact.</summary>
    public async Task<long?> VerifyAsync(CancellationToken ct = default)
    {
        var prev = "";
        await foreach (var e in db.AuditEvents.AsNoTracking().OrderBy(e => e.Seq).AsAsyncEnumerable().WithCancellation(ct))
        {
            if (e.PrevHash != prev || e.Hash != ComputeHash(e.PrevHash, e.AtTicks, e.Actor, e.Action, e.Detail)) return e.Seq;
            prev = e.Hash;
        }
        return null;
    }
}
