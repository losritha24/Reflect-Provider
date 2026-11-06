using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Reflect.ProviderDashboard.Domain.Entities;

namespace Reflect.ProviderDashboard.Web.Data;

/// <summary>
/// Patient and session data, kept apart from the Identity store so clinical data can move to
/// its own (encrypted, backed-up) database without touching sign-in accounts.
/// </summary>
public sealed class ClinicalDbContext(DbContextOptions<ClinicalDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<TherapySession> Sessions => Set<TherapySession>();

    // UTC ticks sort and compare identically on SQLite and Postgres (SQLite cannot ORDER BY DateTimeOffset).
    private static readonly ValueConverter<DateTimeOffset, long> UtcTicks = new(
        v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));
    private static readonly ValueConverter<DateTimeOffset?, long?> NullableUtcTicks = new(
        v => v == null ? null : v.Value.UtcTicks, v => v == null ? null : new DateTimeOffset(v.Value, TimeSpan.Zero));

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Patient>(e =>
        {
            e.ToTable("Patients");
            e.HasKey(p => p.Id);
            e.Property(p => p.AssignedProviderId).HasMaxLength(450);
            e.Property(p => p.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(p => p.ExternalPatientId).HasMaxLength(64).IsRequired();
            e.Property(p => p.NotesSummary).HasMaxLength(2000);
            e.Property(p => p.DateCreated).HasConversion(UtcTicks);
            e.Property(p => p.DateDeleted).HasConversion(NullableUtcTicks);
            e.HasIndex(p => new { p.AssignedProviderId, p.ExternalPatientId }).IsUnique();
            e.HasQueryFilter(p => !p.IsDeleted);
        });
        model.Entity<TherapySession>(e =>
        {
            e.ToTable("Sessions");
            e.HasKey(s => s.Id);
            e.Property(s => s.ModuleName).HasMaxLength(200);
            e.Property(s => s.ReflectionSummary).HasMaxLength(2000);
            e.Property(s => s.StartedAtUtc).HasConversion(UtcTicks);
            e.Property(s => s.DateCreated).HasConversion(UtcTicks);
            e.Property(s => s.DateDeleted).HasConversion(NullableUtcTicks);
            e.HasIndex(s => new { s.PatientId, s.StartedAtUtc });
            e.HasOne<Patient>().WithMany().HasForeignKey(s => s.PatientId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(s => !s.IsDeleted);
        });
    }
}
