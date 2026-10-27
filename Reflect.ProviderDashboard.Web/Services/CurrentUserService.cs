using System.Security.Claims;
using Reflect.ProviderDashboard.Domain.Interfaces;

namespace Reflect.ProviderDashboard.Web.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    public Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : null;

    public string EntraObjectId => User.FindFirstValue("oid") ?? string.Empty;
    public string Email => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
    public bool IsInRole(string role) => User.IsInRole(role);
}
