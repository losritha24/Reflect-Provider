using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Web.Models.Identity;

namespace Reflect.ProviderDashboard.Web.Data;

public sealed class ProviderDbContext : IdentityDbContext<ProviderUser>
{
    public ProviderDbContext(DbContextOptions<ProviderDbContext> options)
        : base(options)
    {
    }
}
