using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Web.Extensions;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;

public class PatientRowViewModel
{
    public Guid PatientId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ExternalPatientId { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public string LastSessionDisplay { get; set; } = "—";
    public string PrimaryPlatformLabel { get; set; } = string.Empty;
    public string TrendLabel { get; set; } = string.Empty;
    public string TrendBadgeColor { get; set; } = "info";

    public static PatientRowViewModel FromEntity(Patient patient, IReadOnlyList<TherapySession> sessions)
    {
        var last = sessions.OrderByDescending(s => s.StartedAtUtc).FirstOrDefault();
        var vrCount = sessions.Count(s => s.Platform == TherapyPlatform.Vr);
        var mobileCount = sessions.Count(s => s.Platform == TherapyPlatform.Mobile);
        var primary = vrCount >= mobileCount ? "Reflect XR (VR)" : "Reflect Mobile";
        if (sessions.Count == 0)
        {
            primary = "No sessions yet";
        }

        var (trendLabel, badge) = patient.Trend switch
        {
            EngagementTrend.Improving => ("Improving", "success"),
            EngagementTrend.Declining => ("Needs follow-up", "warning"),
            _ => ("Stable", "info")
        };

        return new PatientRowViewModel
        {
            PatientId = patient.Id,
            DisplayName = patient.DisplayName,
            ExternalPatientId = patient.ExternalPatientId,
            TotalSessions = sessions.Count,
            LastSessionDisplay = last == null ? "—" : last.StartedAtUtc.ToUsDateString(),
            PrimaryPlatformLabel = primary,
            TrendLabel = trendLabel,
            TrendBadgeColor = badge
        };
    }
}
