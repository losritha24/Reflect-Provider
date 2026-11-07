using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.Identity;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Settings;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.PolicyName)]
public class SettingsController : Controller
{
    private readonly UserManager<ProviderUser> _users;
    private readonly SignInManager<ProviderUser> _signIn;
    private readonly TeamStore _team;
    private readonly ICompanyClock _clock;

    public SettingsController(UserManager<ProviderUser> users, SignInManager<ProviderUser> signIn, TeamStore team, ICompanyClock clock)
    {
        _users = users;
        _signIn = signIn;
        _team = team;
        _clock = clock;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(await BuildAsync(user));
    }

    [HttpPost]
    public async Task<IActionResult> Profile(string displayName, string title, string phone)
    {
        var name = (displayName ?? "").Trim();
        if (name.Length is < 2 or > 100) return await Fail("Enter a name between 2 and 100 characters.");
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        user.DisplayName = name;
        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded) return await Fail("We could not save your profile.");
        return await SaveAsync(user, p => { p.Title = title; p.Phone = phone; }, "Profile saved.");
    }

    [HttpPost]
    public async Task<IActionResult> Display(string theme, string textSize, string landingPage, bool showTour, string timeZone, string clock)
    {
        var zone = (timeZone ?? "").Trim();
        if (zone.Length > 0)
        {
            try { TimeZoneInfo.FindSystemTimeZoneById(zone); }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            { return await Fail("That time zone was not recognized. Use an IANA name such as America/Chicago."); }
        }
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        return await SaveAsync(user, p =>
        {
            p.Theme = theme; p.TextSize = textSize; p.LandingPage = landingPage; p.ShowTour = showTour;
            p.TimeZone = zone; p.Clock = clock;
        }, "Display settings saved.");
    }

    [HttpPost]
    public async Task<IActionResult> Notifications(bool notifyQuieter, bool notifyBaseline, bool notifyMessages, string channel, string quietFrom, string quietTo)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        return await SaveAsync(user, p =>
        {
            p.NotifyQuieter = notifyQuieter; p.NotifyBaseline = notifyBaseline; p.NotifyMessages = notifyMessages;
            p.Channel = channel; p.QuietFrom = quietFrom; p.QuietTo = quietTo;
        }, "Notification preferences saved.");
    }

    private async Task<IActionResult> SaveAsync(ProviderUser user, Action<ProviderPreferences> change, string message)
    {
        var existing = await _users.GetClaimsAsync(user);
        var prefs = ProviderPreferences.From(existing);
        change(prefs);
        var old = existing.Where(c => c.Type.StartsWith(ProviderPreferences.Prefix, StringComparison.Ordinal)).ToList();
        if (old.Count > 0) await _users.RemoveClaimsAsync(user, old);
        await _users.AddClaimsAsync(user, prefs.ToClaims());
        _team.Record(user.Email, "settings-changed", message.TrimEnd('.'));
        // Claims are copied into the cookie at sign-in; re-issue the session so the change is live.
        // (No SignOut first: the ticket store refuses to renew a session removed in the same request.)
        await _signIn.SignInWithClaimsAsync(user, false, new[] { new Claim("display_name", user.DisplayName) });
        TempData["SettingsMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Fail(string error)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        var vm = await BuildAsync(user);
        vm.Error = error;
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View("Index", vm);
    }

    private async Task<SettingsViewModel> BuildAsync(ProviderUser user)
    {
        var prefs = ProviderPreferences.From(await _users.GetClaimsAsync(user));
        return new SettingsViewModel
        {
            DisplayName = user.DisplayName,
            Email = user.Email ?? "",
            Prefs = prefs,
            FacilityName = "Immersive Arts for Wellness",
            FacilityTimeZone = _clock.IanaId,
            IsAdmin = User.IsInRole(ProviderAuthorization.AdminRole),
            Message = TempData["SettingsMessage"] as string,
            Activity = _team.Audit(a => a.Actor.Equals(user.Email, StringComparison.OrdinalIgnoreCase), 15)
        };
    }
}
