using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Account;
using Reflect.ProviderDashboard.Web.Models.Identity;

namespace Reflect.ProviderDashboard.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly UserManager<ProviderUser> _userManager;
    private readonly SignInManager<ProviderUser> _signInManager;
    private readonly IHostEnvironment _environment;

    public AccountController(
        UserManager<ProviderUser> userManager,
        SignInManager<ProviderUser> signInManager,
        IHostEnvironment environment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        if (!_environment.IsDevelopment())
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
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        var result = user is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

        if (!result.Succeeded || user is null)
        {
            var message = result.IsLockedOut
                ? "Too many unsuccessful attempts. Try again in 15 minutes."
                : "The email or password you entered is incorrect.";
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        await _signInManager.SignInWithClaimsAsync(
            user,
            model.RememberMe,
            new[] { new Claim("display_name", user.DisplayName) });
        return RedirectToLocal(model.ReturnUrl);
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
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Dashboard", "Home", new { area = "Provider" });
    }
}
