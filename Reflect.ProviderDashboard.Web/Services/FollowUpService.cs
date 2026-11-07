using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Data;

namespace Reflect.ProviderDashboard.Web.Services;

public sealed record DueFollowUp(FollowUpTask Task, string PatientName, bool Overdue);

/// <summary>
/// Provider-scoped follow-up tasks. Every operation first resolves the patient through the assigned-provider
/// repository, so a task on someone else's patient is a 404 and a client-supplied provider id is never trusted.
/// A task change and its history event are saved in one transaction.
/// </summary>
public sealed class FollowUpService(ClinicalDbContext db, IPatientRepository patients, ICurrentUserService user, TimeProvider clock)
{
    public const int MaxTitle = 120;
    public const int MaxNote = 1000;

    private string ProviderId => ProviderScope.CurrentProviderId(user)
        ?? throw new EntityNotFoundException("Patient not found.");

    public async Task<IReadOnlyList<FollowUpTask>> ListAsync(Guid patientId, CancellationToken ct)
    {
        await patients.GetByIdAsync(patientId, ct);
        var rows = await db.FollowUps.AsNoTracking().Where(f => f.PatientId == patientId).ToListAsync(ct);
        return rows.OrderBy(f => f.Status).ThenBy(f => f.DueAtUtc ?? DateTimeOffset.MaxValue).ThenByDescending(f => f.DateCreated).ToList();
    }

    public async Task<FollowUpTask> CreateAsync(Guid patientId, string title, string note, DateTimeOffset? dueUtc, CancellationToken ct)
    {
        var provider = ProviderId;
        await patients.GetByIdAsync(patientId, ct);
        title = (title ?? "").Trim();
        note = (note ?? "").Trim();
        if (title.Length is 0 or > MaxTitle) throw new ArgumentException($"Title must be 1 to {MaxTitle} characters.");
        if (note.Length > MaxNote) throw new ArgumentException($"Note must be {MaxNote} characters or fewer.");
        var now = clock.GetUtcNow();
        if (dueUtc is { } due && (due < now.AddYears(-1) || due > now.AddYears(2)))
            throw new ArgumentException("Due date must be within the last year and the next two years.");

        var task = new FollowUpTask { PatientId = patientId, Title = title, Note = note, DueAtUtc = dueUtc, CreatedByProviderId = provider, DateCreated = now };
        db.FollowUps.Add(task);
        db.FollowUpEvents.Add(new FollowUpEvent { TaskId = task.Id, PatientId = patientId, Action = "created", ActorProviderId = provider, VersionAfter = 1, DateCreated = now });
        await db.SaveChangesAsync(ct);
        return task;
    }

    /// <summary>Completes, dismisses or reopens a task. <paramref name="expectedVersion"/> must match or the change is refused.</summary>
    public async Task<FollowUpTask> SetStatusAsync(Guid taskId, FollowUpStatus status, int expectedVersion, CancellationToken ct)
    {
        var provider = ProviderId;
        if (!Enum.IsDefined(status)) throw new ArgumentException("Unknown status.");
        var task = await db.FollowUps.FirstOrDefaultAsync(f => f.Id == taskId, ct)
            ?? throw new EntityNotFoundException("Follow-up not found.");
        await patients.GetByIdAsync(task.PatientId, ct);
        if (task.Version != expectedVersion) throw new StaleVersionException("This follow-up changed since you opened it. Reload and try again.");
        if (task.Status == status) return task;

        task.Status = status;
        task.Version++;
        var action = status switch { FollowUpStatus.Completed => "completed", FollowUpStatus.Dismissed => "dismissed", _ => "reopened" };
        db.FollowUpEvents.Add(new FollowUpEvent { TaskId = task.Id, PatientId = task.PatientId, Action = action, ActorProviderId = provider, VersionAfter = task.Version, DateCreated = clock.GetUtcNow() });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new StaleVersionException("This follow-up changed since you opened it. Reload and try again."); }
        return task;
    }

    public async Task<IReadOnlyList<FollowUpEvent>> HistoryAsync(Guid taskId, CancellationToken ct)
    {
        var task = await db.FollowUps.AsNoTracking().FirstOrDefaultAsync(f => f.Id == taskId, ct)
            ?? throw new EntityNotFoundException("Follow-up not found.");
        await patients.GetByIdAsync(task.PatientId, ct);
        var rows = await db.FollowUpEvents.AsNoTracking().Where(e => e.TaskId == taskId).ToListAsync(ct);
        return rows.OrderBy(e => e.DateCreated).ToList();
    }

    /// <summary>Open follow-ups across the provider's caseload that are overdue or due within <paramref name="withinDays"/>.</summary>
    public async Task<IReadOnlyList<DueFollowUp>> DueAsync(int withinDays, CancellationToken ct)
    {
        var all = await patients.GetAllAsync(ct);
        var names = all.ToDictionary(p => p.Id, p => p.DisplayName);
        var ids = names.Keys.ToList();
        var open = await db.FollowUps.AsNoTracking()
            .Where(f => f.Status == FollowUpStatus.Open && ids.Contains(f.PatientId)).ToListAsync(ct);
        var now = clock.GetUtcNow();
        var horizon = now.AddDays(Math.Clamp(withinDays, 0, 90));
        return open.Where(f => f.DueAtUtc is { } due && due <= horizon)
            .OrderBy(f => f.DueAtUtc)
            .Select(f => new DueFollowUp(f, names[f.PatientId], f.DueAtUtc < now)).ToList();
    }

    public async Task<int> OpenCountAsync(CancellationToken ct)
    {
        var ids = (await patients.GetAllAsync(ct)).Select(p => p.Id).ToList();
        return await db.FollowUps.CountAsync(f => f.Status == FollowUpStatus.Open && ids.Contains(f.PatientId), ct);
    }
}
