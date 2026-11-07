using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Domain.Catalog;
using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Web.Data;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.Integration;

namespace Reflect.ProviderDashboard.Web.Services;

public sealed class SessionIngestService
{
    private readonly ClinicalDbContext _db;
    private readonly ICompanyClock _clock;

    public SessionIngestService(ClinicalDbContext db, ICompanyClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<TherapySession> IngestAsync(SessionIngestRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.ExternalPatientId))
            throw new ArgumentException("External patient ID is required.", nameof(request));
        var externalPatientId = request.ExternalPatientId.Trim();
        if (externalPatientId.Length > 64)
            throw new ArgumentException("External patient ID must be 64 characters or fewer.", nameof(request));
        if (request.StartedAt == default)
            throw new ArgumentException("Session start time is required.", nameof(request));
        if (request.DurationMinutes <= 0 || request.DurationMinutes > 24 * 60)
            throw new ArgumentException("Duration must be between 1 and 1440 minutes.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ModuleName))
            throw new ArgumentException("Module name is required.", nameof(request));
        if ((request.ReflectionSummary?.Length ?? 0) > 2000)
            throw new ArgumentException("Reflection summary must be 2000 characters or fewer.", nameof(request));
        if (request.AffectScore is < 1 or > 10)
            throw new ArgumentException("Affect score must be 1–10 when supplied.", nameof(request));
        if (!TryParsePlatform(request.Platform, out var platform))
            throw new ArgumentException("Platform must be Vr or Mobile.", nameof(request));
        var module = ReflectModuleCatalog.Find(platform, request.ModuleName)
            ?? throw new ArgumentException($"Module is not in the Reflect {(platform == TherapyPlatform.Vr ? "XR" : "Mobile")} catalog.", nameof(request));

        var normalizedPatientId = externalPatientId.ToLower();
        var patient = await _db.Patients.FirstOrDefaultAsync(
            candidate => candidate.ExternalPatientId.ToLower() == normalizedPatientId, ct);
        if (patient is null)
            throw new EntityNotFoundException("Patient not found.");

        Guid? deviceId = null;
        if (!string.IsNullOrWhiteSpace(request.HardwareId))
        {
            var device = InMemoryDataStore.Devices.FirstOrDefault(candidate =>
                !candidate.IsDeleted &&
                candidate.HardwareId.Equals(request.HardwareId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (device is null)
                throw new EntityNotFoundException("Device not found.");
            deviceId = device.Id;
        }

        var session = new TherapySession
        {
            PatientId = patient.Id,
            StartedAtUtc = _clock.ToUtc(request.StartedAt),
            DurationMinutes = request.DurationMinutes,
            Platform = platform,
            ModuleName = module.Name,
            ReflectionSummary = (request.ReflectionSummary ?? string.Empty).Trim(),
            AffectScore = request.AffectScore,
            DeviceId = deviceId
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    private static bool TryParsePlatform(string value, out TherapyPlatform platform)
    {
        platform = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (value.Equals("Vr", StringComparison.OrdinalIgnoreCase)
            || value.Equals("XR", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Reflect XR", StringComparison.OrdinalIgnoreCase))
        {
            platform = TherapyPlatform.Vr;
            return true;
        }
        if (value.Equals("Mobile", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Reflect Mobile", StringComparison.OrdinalIgnoreCase))
        {
            platform = TherapyPlatform.Mobile;
            return true;
        }
        return false;
    }
}
