using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

/// <summary>Create, complete, dismiss and reopen follow-ups. Antiforgery is enforced globally; scoping is in the service.</summary>
[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.PolicyName)]
public class FollowUpsController : Controller
{
    private readonly FollowUpService _followUps;
    private readonly AuditLog _audit;

    public FollowUpsController(FollowUpService followUps, AuditLog audit)
    {
        _followUps = followUps;
        _audit = audit;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid patientId, string title, string note, DateOnly? due, CancellationToken ct)
    {
        if (patientId == Guid.Empty) return NotFound();
        try
        {
            DateTimeOffset? dueUtc = due is { } d ? new DateTimeOffset(d.ToDateTime(new TimeOnly(17, 0)), TimeSpan.Zero) : null;
            var task = await _followUps.CreateAsync(patientId, title, note, dueUtc, ct);
            await Record("followup-created", task.Id, ct);
            TempData["FollowUpNotice"] = "Follow-up added.";
        }
        catch (EntityNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { TempData["FollowUpError"] = ex.Message; }
        return Back(patientId);
    }

    [HttpPost]
    public async Task<IActionResult> Update(Guid id, Guid patientId, string action, int version, CancellationToken ct)
    {
        var status = action switch
        {
            "complete" => FollowUpStatus.Completed,
            "dismiss" => FollowUpStatus.Dismissed,
            "reopen" => FollowUpStatus.Open,
            _ => (FollowUpStatus?)null
        };
        if (id == Guid.Empty || status is null) return BadRequest();
        try
        {
            var task = await _followUps.SetStatusAsync(id, status.Value, version, ct);
            await Record("followup-" + action, task.Id, ct);
            return Back(task.PatientId);
        }
        catch (EntityNotFoundException) { return NotFound(); }
        catch (StaleVersionException ex)
        {
            TempData["FollowUpError"] = ex.Message;
            return patientId == Guid.Empty ? RedirectToAction("Dashboard", "Home") : Back(patientId);
        }
    }

    private Task Record(string action, Guid taskId, CancellationToken ct) =>
        _audit.RecordAsync(User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "provider", action, taskId.ToString("N")[..8], ct);

    private IActionResult Back(Guid patientId) =>
        RedirectToAction("Details", "Patients", new { id = patientId });
}
