using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.Integration;

namespace Reflect.ProviderDashboard.Web.Services;

public sealed class SessionIngestService
{
    private readonly ICompanyClock _clock;

    public SessionIngestService(ICompanyClock clock)
    {
        _clock = clock;
    }

    public TherapySession Ingest(SessionIngestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ExternalPatientId))
            throw new ArgumentException("External patient ID is required.", nameof(request));
        if (request.DurationMinutes <= 0 || request.DurationMinutes > 24 * 60)
            throw new ArgumentException("Duration must be between 1 and 1440 minutes.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ModuleName))
            throw new ArgumentException("Module name is required.", nameof(request));
        if (request.AffectScore is < 1 or > 10)
            throw new ArgumentException("Affect score must be 1–10 when supplied.", nameof(request));
        if (!TryParsePlatform(request.Platform, out var platform))
            throw new ArgumentException("Platform must be Vr or Mobile.", nameof(request));

        var patient = InMemoryDataStore.Patients.FirstOrDefault(candidate =>
            !candidate.IsDeleted &&
            candidate.ExternalPatientId.Equals(request.ExternalPatientId.Trim(), StringComparison.OrdinalIgnoreCase));
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
            ModuleName = request.ModuleName.Trim(),
            ReflectionSummary = (request.ReflectionSummary ?? string.Empty).Trim(),
            AffectScore = request.AffectScore,
            DeviceId = deviceId
        };
        InMemoryDataStore.Sessions.Add(session);
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
