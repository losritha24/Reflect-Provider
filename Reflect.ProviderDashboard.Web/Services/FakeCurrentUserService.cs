using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Services;

public class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
    public string EntraObjectId => "demo-provider-entra-id";
    public string Email => "dr.morgan@demo-clinic.example";
    public bool IsAuthenticated => true;

    public bool IsInRole(string role) =>
        string.Equals(role, ProviderAuthorization.ProviderRole, StringComparison.OrdinalIgnoreCase);
}
