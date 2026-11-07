using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Messages;
using Reflect.ProviderDashboard.Web.Services;
using Reflect.ProviderDashboard.Web.Services.Chat;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

/// <summary>
/// Prototype outbox for care-team nudges. Messages are queued only: nothing is delivered to
/// Reflect Mobile or XR yet, and no push is sent (see docs/TODO.md section D).
/// </summary>
[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.PolicyName)]
public class MessagesController : Controller
{
    private readonly IPatientRepository _patients;
    private readonly ICurrentUserService _user;
    private readonly TeamStore _team;
    private readonly AuditLog _audit;

    public MessagesController(IPatientRepository patients, ICurrentUserService user, TeamStore team, AuditLog audit)
    {
        _patients = patients;
        _user = user;
        _team = team;
        _audit = audit;
    }

    public async Task<IActionResult> Index(Guid? patientId, CancellationToken ct)
    {
        var vm = await BuildAsync(ct);
        vm.PatientId = patientId;
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Send(Guid patientId, string template, string body, CancellationToken ct)
    {
        var text = (body ?? "").Trim();
        if (text.Length == 0 && MessageTemplates.All.TryGetValue(template ?? "", out var preset)) text = preset;
        var guard = ChatGuardrail.Inspect(text);
        if (!guard.Allowed) return await Fail(patientId, guard.Message, ct);

        Domain.Entities.Patient patient;
        try { patient = await _patients.GetByIdAsync(patientId, ct); }
        catch (EntityNotFoundException) { return NotFound(); }

        var providerId = ProviderScope.CurrentProviderId(_user);
        _team.Queue(providerId, patient.Id, patient.DisplayName, guard.Sanitized);
        // The audit trail records that a message was sent, never its content or the patient's name.
        await _audit.RecordAsync(User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "provider", "message-queued", patient.ExternalPatientId, ct);
        TempData["MessageNotice"] = guard.Redactions.Count > 0
            ? $"Queued. We removed possible identifiers from your text ({string.Join(", ", guard.Redactions)}). Nothing is delivered in this prototype."
            : "Queued. Nothing is delivered in this prototype.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Fail(Guid patientId, string error, CancellationToken ct)
    {
        var vm = await BuildAsync(ct);
        vm.PatientId = patientId;
        vm.Error = error;
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View("Index", vm);
    }

    private async Task<MessagesViewModel> BuildAsync(CancellationToken ct) => new()
    {
        Patients = await _patients.GetAllAsync(ct),
        Outbox = _team.Outbox(ProviderScope.CurrentProviderId(_user) ?? ""),
        Notice = TempData["MessageNotice"] as string
    };
}
