using Microsoft.AspNetCore.Identity;

namespace Reflect.ProviderDashboard.Web.Models.Identity;

public sealed class ProviderUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
