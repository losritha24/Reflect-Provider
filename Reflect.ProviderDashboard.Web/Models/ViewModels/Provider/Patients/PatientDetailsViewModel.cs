using Reflect.ProviderDashboard.Domain.Catalog;
using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Services;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;

public class PatientDetailsViewModel
{
    public Guid PatientId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ExternalPatientId { get; set; } = string.Empty;
    public string DateOfBirthDisplay { get; set; } = string.Empty;
    public string TrendLabel { get; set; } = string.Empty;
    public string TrendBadgeColor { get; set; } = "info";
    public string NotesSummary { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int VrSessionCount { get; set; }
    public int MobileSessionCount { get; set; }
    public int TotalMinutes { get; set; }
    public int FilteredSessionCount => Sessions.Count;
    public TherapyPlatform? Platform { get; set; }
    public int? Days { get; set; }
    public DateTimeOffset AsOfUtc { get; private set; }
    public string Search { get; set; } = string.Empty;
    public EngagementTrend? Trend { get; set; }
    public int Page { get; set; } = 1;
    public bool TimelineSelected => Tab == "timeline";
    public string Tab { get; set; } = "overview";
    public List<PatientSessionRowViewModel> Sessions { get; set; } = new();
    public EmotionalTrajectory Trajectory { get; set; } = new(Array.Empty<AffectPoint>(), false, null, null);
    public string TrajectoryChartSvg { get; set; } = string.Empty;
    public List<CareDevice> Devices { get; set; } = new();
    public List<ProgramUsageRow> ProgramUsage { get; set; } = new();

    public sealed record ProgramUsageRow(string Group, int Sessions, IReadOnlyList<string> Modules);

    public async Task PrepareData(
        Guid patientId,
        IPatientRepository patients,
        ITherapySessionRepository sessions,
        ICareDeviceRepository devices,
        CancellationToken ct,
        DateTimeOffset asOfUtc,
        TherapyPlatform? platform = null,
        int? days = null,
        string search = null,
        EngagementTrend? trend = null,
        int page = 1,
        string tab = "overview")
    {
        AsOfUtc = asOfUtc;
        Platform = platform;
        Days = days;
        Search = search ?? string.Empty;
        Trend = trend;
        Page = page;
        Tab = platform.HasValue || days.HasValue ? "timeline" : tab;
        var patient = await patients.GetByIdAsync(patientId, ct);
        PatientId = patient.Id;
        DisplayName = patient.DisplayName;
        ExternalPatientId = patient.ExternalPatientId;
        DateOfBirthDisplay = patient.DateOfBirth.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        NotesSummary = patient.NotesSummary;

        (TrendLabel, TrendBadgeColor) = patient.Trend switch
        {
            Domain.Enums.EngagementTrend.Improving => ("Improving engagement", "success"),
            Domain.Enums.EngagementTrend.Declining => ("Declining engagement", "warning"),
            _ => ("Stable engagement", "info")
        };

        var patientSessions = (await sessions.GetByPatientIdAsync(patientId, ct))
            .Where(s => s.StartedAtUtc <= AsOfUtc)
            .ToList();
        TotalSessions = patientSessions.Count;
        VrSessionCount = patientSessions.Count(s => s.Platform == TherapyPlatform.Vr);
        MobileSessionCount = patientSessions.Count(s => s.Platform == TherapyPlatform.Mobile);
        TotalMinutes = patientSessions.Sum(s => s.DurationMinutes);
        ProgramUsage = patientSessions
            .GroupBy(s => ReflectModuleCatalog.Find(s.Platform, s.ModuleName)?.Group ?? "Other")
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Select(g => new ProgramUsageRow(g.Key, g.Count(), g.Select(s => s.ModuleName).Distinct().OrderBy(n => n).ToList()))
            .ToList();

        var timeline = patientSessions.AsEnumerable();
        if (Platform.HasValue) timeline = timeline.Where(s => s.Platform == Platform.Value);
        if (Days.HasValue) timeline = timeline.Where(s => s.StartedAtUtc >= AsOfUtc.AddDays(-Days.Value));
        Sessions = timeline.OrderByDescending(s => s.StartedAtUtc).Select(PatientSessionRowViewModel.FromEntity).ToList();
        Trajectory = EmotionalTrajectoryCalculator.Build(patientSessions);
        TrajectoryChartSvg = AffectChartSvg.From(Trajectory);
        Devices = (await devices.GetByPatientIdAsync(patientId, ct)).ToList();
    }
}
