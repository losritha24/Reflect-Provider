using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Web.Extensions;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;

public class PatientSessionRowViewModel
{
    public string SessionDateTimeDisplay { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string PlatformLabel { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string ReflectionSummary { get; set; } = string.Empty;

    public static PatientSessionRowViewModel FromEntity(TherapySession session)
    {
        return new PatientSessionRowViewModel
        {
            SessionDateTimeDisplay = session.StartedAtUtc.ToUsDateTimeString(),
            DurationMinutes = session.DurationMinutes,
            PlatformLabel = session.Platform == TherapyPlatform.Vr ? "Reflect XR (VR)" : "Reflect Mobile",
            ModuleName = session.ModuleName,
            ReflectionSummary = session.ReflectionSummary
        };
    }
}
