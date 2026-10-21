using Reflect.ProviderDashboard.Domain.Interfaces;

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
    public List<PatientSessionRowViewModel> Sessions { get; set; } = new();

    public async Task PrepareData(
        Guid patientId,
        IPatientRepository patients,
        ITherapySessionRepository sessions,
        CancellationToken ct)
    {
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

        var patientSessions = await sessions.GetByPatientIdAsync(patientId, ct);
        TotalSessions = patientSessions.Count;
        VrSessionCount = patientSessions.Count(s => s.Platform == Domain.Enums.TherapyPlatform.Vr);
        MobileSessionCount = patientSessions.Count(s => s.Platform == Domain.Enums.TherapyPlatform.Mobile);
        TotalMinutes = patientSessions.Sum(s => s.DurationMinutes);
        Sessions = patientSessions.Select(PatientSessionRowViewModel.FromEntity).ToList();
    }
}
