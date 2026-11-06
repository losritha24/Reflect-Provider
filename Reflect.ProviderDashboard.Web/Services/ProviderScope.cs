using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Services;

/// <summary>The one place that decides whose caseload a request may see. Fails closed.</summary>
internal static class ProviderScope
{
    public static string CurrentProviderId(ICurrentUserService user) =>
        user.IsAuthenticated && user.IsInRole(ProviderAuthorization.ProviderRole)
            ? user.UserId?.ToString() : null;
}
