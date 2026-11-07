using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Account;
using Reflect.ProviderDashboard.Web.Models.Identity;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Controllers;

[RequestSizeLimit(16_384)]
public sealed class AccountController : Controller
{
    private readonly UserManager<ProviderUser> _userManager;
    private readonly SignInManager<ProviderUser> _signInManager;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly TeamStore _team;
    private bool LoginEnabled => _environment.IsDevelopment() || _configuration.GetValue<bool>("Demo:Enabled");

    public AccountController(
        UserManager<ProviderUser> userManager,
        SignInManager<ProviderUser> signInManager,
        IHostEnvironment environment, IConfiguration configuration, TeamStore team)
    {
        _team = team;
        _userManager = userManager;
        _signInManager = signInManager;
        _environment = environment;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        if (!LoginEnabled)
        {
            return NotFound();
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("account")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!LoginEnabled) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        // Hosted: only the demo login and accounts created from an admin invite may sign in.
        var allowedHere = _environment.IsDevelopment()
            || string.Equals(model.Email.Trim(), _configuration["Demo:Email"]?.Trim(), StringComparison.OrdinalIgnoreCase)
            || (user is not null && (await _userManager.GetClaimsAsync(user)).Any(c => c.Type == ProviderAuthorization.InvitedClaim));
        if (!allowedHere) user = null;
        var result = user is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

        if (!result.Succeeded || user is null)
        {
            var message = "Sign-in failed. Check your credentials or try again later.";
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        await _signInManager.SignInWithClaimsAsync(
            user,
            model.RememberMe,
            new[] { new Claim("display_name", user.DisplayName) });
        _team.Record(user.Email, "sign-in", "Signed in");
        return RedirectToLocal(model.ReturnUrl, await LandingAsync(user));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AcceptInvite(string token)
    {
        if (!LoginEnabled) return NotFound();
        var invite = _team.FindUsable(token);
        if (invite is null) return InviteGone();
        return View(new AcceptInviteViewModel { Token = token, Email = invite.Email, DisplayName = invite.DisplayName });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("account")]
    public async Task<IActionResult> AcceptInvite(AcceptInviteViewModel model)
    {
        if (!LoginEnabled) return NotFound();
        var invite = _team.FindUsable(model.Token);
        if (invite is null) return InviteGone();
        model.Email = invite.Email;
        if (!ModelState.IsValid) return View(model);

        var user = new ProviderUser { UserName = invite.Email, Email = invite.Email, DisplayName = model.DisplayName.Trim() };
        var created = await _userManager.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        var claim = await _userManager.AddClaimAsync(user, new Claim(ProviderAuthorization.InvitedClaim, invite.CreatedBy));
        var role = claim.Succeeded ? await _userManager.AddToRoleAsync(user, ProviderAuthorization.ProviderRole) : claim;
        if (!role.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "We could not finish creating your account. Ask your administrator for a new invite.");
            return View(model);
        }
        _team.MarkAccepted(invite.Id);
        _team.Record(user.Email, "invite-accepted", "Created account from invite");
        await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, new[] { new Claim("display_name", user.DisplayName) });
        return RedirectToLocal(null);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string returnUrl = null)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("account")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = new ProviderUser
        {
            UserName = email,
            Email = email,
            DisplayName = model.DisplayName.Trim()
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, ProviderAuthorization.ProviderRole);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "We could not finish creating your account. Please try again.");
            return View(model);
        }

        await _signInManager.SignInWithClaimsAsync(
            user,
            isPersistent: false,
            new[] { new Claim("display_name", user.DisplayName) });
        return RedirectToLocal(model.ReturnUrl);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    private IActionResult InviteGone()
    {
        Response.StatusCode = StatusCodes.Status410Gone;
        return View("InviteInvalid");
    }

    private async Task<string> LandingAsync(ProviderUser user) =>
        ProviderPreferences.From(await _userManager.GetClaimsAsync(user)).LandingPage;

    private IActionResult RedirectToLocal(string returnUrl, string landing = "Dashboard")
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return landing switch
        {
            "Patients" => RedirectToAction("Index", "Patients", new { area = "Provider" }),
            "Graph" => RedirectToAction("Graph", "Home", new { area = "Provider" }),
            "Messages" => RedirectToAction("Index", "Messages", new { area = "Provider" }),
            _ => RedirectToAction("Dashboard", "Home", new { area = "Provider" })
        };
    }
}
