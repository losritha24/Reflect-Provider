using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Web.Data;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.Identity;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Team;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

/// <summary>Facility administration: people and assignments. Never exposes names or sessions of patients.</summary>
[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.AdminPolicyName)]
public class TeamController : Controller
{
    private readonly UserManager<ProviderUser> _users;
    private readonly PatientAssignmentService _assignments;
    private readonly TeamStore _team;
    private readonly AuditLog _audit;
    private readonly IConfiguration _configuration;
    private readonly ClinicalDbContext _clinical;

    public TeamController(UserManager<ProviderUser> users, PatientAssignmentService assignments, TeamStore team, AuditLog audit, IConfiguration configuration, ClinicalDbContext clinical)
    {
        _clinical = clinical;
        _configuration = configuration;
        _users = users;
        _assignments = assignments;
        _team = team;
        _audit = audit;
    }

    // Optional hard requirement: admins must have two-step sign-in before managing people.
    public override async Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
    {
        if (_configuration.GetValue<bool>("Security:RequireMfaForAdmins"))
        {
            var user = await _users.GetUserAsync(User);
            if (user is not null && !user.TwoFactorEnabled)
            {
                context.Result = RedirectToAction("Mfa", "Settings");
                return;
            }
        }
        await next();
    }

    public async Task<IActionResult> Index(CancellationToken ct) => View(await BuildAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Invite(string email, string displayName, CancellationToken ct)
    {
        var address = (email ?? "").Trim();
        var name = (displayName ?? "").Trim();
        if (name.Length is < 2 or > 100 || !new EmailAddressAttribute().IsValid(address) || address.Length > 254)
            return await WithError("Enter a name and a valid work email.", ct);
        if (await _users.FindByEmailAsync(address) is not null)
            return await WithError("An account with that email already exists.", ct);

        var token = _team.CreateInvite(address, name, ActorEmail, out var invite);
        await _audit.RecordAsync(ActorEmail, "invite-created", $"Invited {invite.Email}");
        var vm = await BuildAsync(ct);
        vm.NewInviteLink = Url.Action("AcceptInvite", "Account", new { area = "", token }, Request.Scheme);
        vm.NewInviteEmail = invite.Email;
        return View("Index", vm);
    }

    /// <summary>Disposable demo only: rebuild the synthetic data with fresh dates and put every patient back with the signed-in admin.</summary>
    [HttpPost]
    public async Task<IActionResult> ResetDemo(CancellationToken ct)
    {
        if (!_configuration.GetValue<bool>("Demo:Enabled")) return NotFound();
        var adminId = _users.GetUserId(User);
        if (string.IsNullOrEmpty(adminId)) return Forbid();
        InMemoryDataStore.Seed();
        InMemoryDataStore.AssignPatientsToProvider(adminId);
        await ClinicalSeeder.EnsureAsync(_clinical, adminId, reseed: true, ct);
        _team.Clear();
        await _audit.RecordAsync(ActorEmail, "demo-reset", "Synthetic data rebuilt", ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Revoke(Guid id)
    {
        if (_team.Revoke(id)) await _audit.RecordAsync(ActorEmail, "invite-revoked", id.ToString("N")[..8]);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Assign(Guid patientId, string providerId, CancellationToken ct)
    {
        providerId = (providerId ?? "").Trim();
        if (providerId.Length > 0)
        {
            var target = await _users.FindByIdAsync(providerId);
            if (target is null || !await _users.IsInRoleAsync(target, ProviderAuthorization.ProviderRole)
                || await _users.IsLockedOutAsync(target))
                return await WithError("Choose an active provider.", ct);
        }
        try
        {
            var previous = await _assignments.AssignAsync(patientId, providerId, ct);
            await _audit.RecordAsync(ActorEmail, "patient-assigned", $"Patient moved {(previous.Length == 0 ? "from unassigned" : "between providers")}{(providerId.Length == 0 ? " to unassigned" : "")}");
        }
        catch (EntityNotFoundException) { return NotFound(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(string id, bool active, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(id ?? "");
        if (user is null) return NotFound();
        if (user.Id == _users.GetUserId(User)) return await WithError("You cannot deactivate your own account.", ct);
        await _users.SetLockoutEnabledAsync(user, true);
        await _users.SetLockoutEndDateAsync(user, active ? null : DateTimeOffset.MaxValue);
        // Existing sessions end at their next security-stamp check or within the one-hour cookie lifetime.
        await _users.UpdateSecurityStampAsync(user);
        await _audit.RecordAsync(ActorEmail, active ? "account-reactivated" : "account-deactivated", user.Email ?? "");
        return RedirectToAction(nameof(Index));
    }

    private string ActorEmail => User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        ?? User.Identity?.Name ?? "admin";

    private async Task<IActionResult> WithError(string error, CancellationToken ct)
    {
        var vm = await BuildAsync(ct);
        vm.Error = error;
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View("Index", vm);
    }

    private async Task<TeamViewModel> BuildAsync(CancellationToken ct)
    {
        var counts = await _assignments.CountsAsync(ct);
        var providers = new List<TeamMember>();
        foreach (var user in (await _users.GetUsersInRoleAsync(ProviderAuthorization.ProviderRole)).OrderBy(u => u.DisplayName))
        {
            counts.TryGetValue(user.Id, out var patients);
            providers.Add(new TeamMember(user.Id, user.DisplayName, user.Email ?? "",
                await _users.IsInRoleAsync(user, ProviderAuthorization.AdminRole) ? "Provider + admin" : "Provider",
                !await _users.IsLockedOutAsync(user), patients, user.Id == _users.GetUserId(User)));
        }
        return new TeamViewModel
        {
            Providers = providers,
            Invites = _team.Invites(),
            Patients = await _assignments.ListAsync(ct),
            Audit = await _audit.QueryAsync(null, 30, ct),
            CanResetDemo = _configuration.GetValue<bool>("Demo:Enabled"),
            Now = DateTimeOffset.UtcNow
        };
    }
}
