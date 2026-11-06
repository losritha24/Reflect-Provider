using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Reports;

public sealed class ClinicalReportViewModel
{
    public IReadOnlyList<Patient> Patients { get; private set; } = Array.Empty<Patient>();
    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; } = string.Empty;
    public string ExternalPatientId { get; private set; } = string.Empty;
    public string NotesSummary { get; private set; } = string.Empty;
    public DateOnly From { get; private set; }
    public DateOnly To { get; private set; }
    public string ReportingPeriod { get; private set; } = string.Empty;
    public string MainModule { get; private set; } = "No sessions in range";
    public string ClinicianName { get; private set; } = string.Empty;
    public string PreparedOn { get; private set; } = string.Empty;
    public int SessionCount { get; private set; }
    public int TotalMinutes { get; private set; }
    public int XrCount { get; private set; }
    public int MobileCount { get; private set; }
    public bool IncludeSummary { get; private set; }
    public bool IncludeHistory { get; private set; }
    public bool IncludeSplit { get; private set; }
    public bool HasCaseload { get; private set; }
    public List<ReportSessionRow> Sessions { get; } = new();

    public string PlatformSplitLabel => $"{XrCount} XR / {MobileCount} Mobile";

    public async Task PrepareData(
        IPatientRepository patients,
        ITherapySessionRepository sessions,
        ICompanyClock clock,
        CancellationToken ct,
        Guid? patientId,
        DateOnly from,
        DateOnly to,
        bool includeSummary,
        bool includeHistory,
        bool includeSplit,
        string clinicianName)
    {
        IncludeSummary = includeSummary;
        IncludeHistory = includeHistory;
        IncludeSplit = includeSplit;
        ClinicianName = string.IsNullOrWhiteSpace(clinicianName) ? "Assigned clinician" : clinicianName.Trim();
        From = from;
        To = to;
        ReportingPeriod = $"{from:M/d/yyyy} – {to:M/d/yyyy}";
        PreparedOn = clock.FormatDate(DateTimeOffset.UtcNow);

        Patients = (await patients.GetAllAsync(ct)).OrderBy(row => row.DisplayName).ToList();
        HasCaseload = Patients.Count > 0;
        Patient patient;
        if (patientId is { } requestedId && requestedId != Guid.Empty)
            patient = await patients.GetByIdAsync(requestedId, ct);
        else if (!HasCaseload)
            return;
        else
            patient = Patients[0];

        PatientId = patient.Id;
        PatientName = patient.DisplayName;
        ExternalPatientId = patient.ExternalPatientId;
        NotesSummary = patient.NotesSummary;

        var startUtc = clock.StartOfCompanyDay(from);
        var endUtc = clock.StartOfCompanyDay(to.AddDays(1));
        var now = DateTimeOffset.UtcNow;
        var inRange = (await sessions.GetByPatientIdAsync(patient.Id, ct))
            .Where(session => session.StartedAtUtc >= startUtc
                && session.StartedAtUtc < endUtc
                && session.StartedAtUtc <= now)
            .OrderByDescending(session => session.StartedAtUtc)
            .ToList();

        SessionCount = inRange.Count;
        TotalMinutes = inRange.Sum(session => session.DurationMinutes);
        XrCount = inRange.Count(session => session.Platform == TherapyPlatform.Vr);
        MobileCount = inRange.Count(session => session.Platform == TherapyPlatform.Mobile);
        MainModule = inRange
            .GroupBy(session => session.ModuleName)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Key)
            .FirstOrDefault() ?? "No sessions in range";

        foreach (var session in inRange)
        {
            Sessions.Add(new ReportSessionRow(
                clock.FormatDate(session.StartedAtUtc),
                session.Platform == TherapyPlatform.Vr ? "Reflect XR" : "Reflect Mobile",
                session.ModuleName,
                session.DurationMinutes,
                session.ReflectionSummary));
        }
    }

    public sealed record ReportSessionRow(
        string Date,
        string Platform,
        string Module,
        int DurationMinutes,
        string Note);
}
