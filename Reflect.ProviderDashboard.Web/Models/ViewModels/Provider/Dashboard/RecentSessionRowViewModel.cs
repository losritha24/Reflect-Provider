using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Web.Extensions;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Dashboard;

public class RecentSessionRowViewModel
{
    public Guid SessionId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string SessionDateTimeDisplay { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string PlatformLabel { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;

    public static RecentSessionRowViewModel FromEntity(TherapySession session, string patientName)
    {
        return new RecentSessionRowViewModel
        {
            SessionId = session.Id,
            PatientId = session.PatientId,
            PatientName = patientName,
            SessionDateTimeDisplay = session.StartedAtUtc.ToUsDateTimeString(),
            DurationMinutes = session.DurationMinutes,
            PlatformLabel = session.Platform == TherapyPlatform.Vr ? "Reflect XR (VR)" : "Reflect Mobile",
            ModuleName = session.ModuleName
        };
    }
}
