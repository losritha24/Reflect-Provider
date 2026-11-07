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

    public DbSet<Reflect.ProviderDashboard.Web.Services.AuditEvent> AuditEvents => Set<Reflect.ProviderDashboard.Web.Services.AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Reflect.ProviderDashboard.Web.Services.AuditEvent>(e =>
        {
            e.ToTable("AuditEvents");
            e.HasKey(x => x.Seq);
            e.Property(x => x.Seq).ValueGeneratedOnAdd();
            e.Property(x => x.Actor).HasMaxLength(256);
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.Detail).HasMaxLength(500);
            e.Property(x => x.PrevHash).HasMaxLength(64);
            e.Property(x => x.Hash).HasMaxLength(64);
            e.HasIndex(x => x.Actor);
        });
    }
}
