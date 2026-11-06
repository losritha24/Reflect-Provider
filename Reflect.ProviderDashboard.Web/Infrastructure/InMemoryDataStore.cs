using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;

namespace Reflect.ProviderDashboard.Web.Infrastructure;

public static class InMemoryDataStore
{
    public static IReadOnlyList<Patient> Patients { get; private set; } = new List<Patient>();
    public static List<TherapySession> Sessions { get; private set; } = new();
    public static List<CareDevice> Devices { get; private set; } = new();
    public static List<DeviceAssignment> DeviceAssignments { get; private set; } = new();

    public static DateTimeOffset SeededAtUtc { get; private set; }

    public static readonly Guid HeadsetTaylorId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1");
    public static readonly Guid HeadsetSamId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd2");
    public static readonly Guid HeadsetUnassignedId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd3");
    public static readonly Guid MobileJordanId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd4");
    public static readonly Guid MobileCaseyId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd5");
    public static readonly Guid MobileAlexId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd6");

    public static void AssignPatientsToProvider(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("A provider ID is required.", nameof(providerId));

        foreach (var patient in Patients)
            patient.AssignedProviderId = providerId;
    }

    public static void Seed()
    {
        var patients = new List<Patient>
        {
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                DisplayName = "Alex Rivera",
                ExternalPatientId = "PT-10041",
                DateOfBirth = new DateOnly(1994, 3, 12),
                Trend = EngagementTrend.Improving,
                NotesSummary = "Increased VR use; reports clearer emotional labeling after Mandala module."
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                DisplayName = "Jordan Kim",
                ExternalPatientId = "PT-10052",
                DateOfBirth = new DateOnly(1988, 7, 22),
                Trend = EngagementTrend.Stable,
                NotesSummary = "Steady mobile journaling; prefers evening sessions."
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"),
                DisplayName = "Sam Patel",
                ExternalPatientId = "PT-10063",
                DateOfBirth = new DateOnly(2001, 11, 5),
                Trend = EngagementTrend.Declining,
                NotesSummary = "Session frequency dropped over the last two weeks; follow up in clinic."
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"),
                DisplayName = "Taylor Brooks",
                ExternalPatientId = "PT-10074",
                DateOfBirth = new DateOnly(1979, 1, 30),
                Trend = EngagementTrend.Improving,
                NotesSummary = "Strong engagement with VR grounding exercises."
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"),
                DisplayName = "Casey Nguyen",
                ExternalPatientId = "PT-10085",
                DateOfBirth = new DateOnly(1996, 9, 18),
                Trend = EngagementTrend.Stable,
                NotesSummary = "Balanced VR and mobile use; working through grief narrative module."
            }
        };

        var now = DateTimeOffset.UtcNow;
        SeededAtUtc = now;
        var alex = patients[0].Id;
        var jordan = patients[1].Id;
        var sam = patients[2].Id;
        var taylor = patients[3].Id;
        var casey = patients[4].Id;

        var sessions = new List<TherapySession>
        {
            Session(alex, now.AddDays(-18), 20, TherapyPlatform.Mobile, "Mood Check-in", "Rated the week as manageable.", 5, MobileAlexId),
            Session(alex, now.AddDays(-12), 24, TherapyPlatform.Vr, "Mandala Emotions", "Began linking color choices to mood.", 6, HeadsetUnassignedId),
            Session(alex, now.AddDays(-8), 35, TherapyPlatform.Vr, "Safe Place VR", "Built coastal scene; reported 6/10 calm post-session.", 6, HeadsetUnassignedId),
            Session(alex, now.AddDays(-4), 22, TherapyPlatform.Mobile, "Guided Reflection", "Wrote about workplace stressors and coping plan.", 7, MobileAlexId),
            Session(alex, now.AddDays(-1).AddHours(-2), 28, TherapyPlatform.Vr, "Mandala Emotions", "Named anxiety as a color wheel; chose cool tones for regulation.", 7, HeadsetUnassignedId),

            Session(jordan, now.AddDays(-16), 14, TherapyPlatform.Mobile, "Mood Check-in", "Evening check-in after work.", 5, MobileJordanId),
            Session(jordan, now.AddDays(-10), 16, TherapyPlatform.Mobile, "Guided Reflection", "Wrote about sleep routine.", 5, MobileJordanId),
            Session(jordan, now.AddDays(-6), 15, TherapyPlatform.Mobile, "AI Imagery Journal", "Generated image for 'hope'; short narrative attached.", 6, MobileJordanId),
            Session(jordan, now.AddDays(-2), 18, TherapyPlatform.Mobile, "Mood Check-in", "Mood 5→7 after gratitude prompt.", 7, MobileJordanId),

            Session(sam, now.AddDays(-28), 26, TherapyPlatform.Vr, "Safe Place VR", "Completed the full scene build.", 7, HeadsetSamId),
            Session(sam, now.AddDays(-24), 22, TherapyPlatform.Mobile, "Mood Check-in", "Described a typical week.", 7, null),
            Session(sam, now.AddDays(-20), 24, TherapyPlatform.Vr, "Grounding Garden", "Stayed with the exercise.", 6, HeadsetSamId),
            Session(sam, now.AddDays(-16), 18, TherapyPlatform.Mobile, "Guided Reflection", "Shorter entry than usual.", 7, null),
            Session(sam, now.AddDays(-14), 25, TherapyPlatform.Vr, "Body Scan VR", "Ended session early due to discomfort.", 5, HeadsetSamId),
            Session(sam, now.AddDays(-12), 20, TherapyPlatform.Mobile, "Sleep Wind-down", "Completed breathing; did not finish reflection.", 4, null),

            Session(taylor, now.AddDays(-10), 28, TherapyPlatform.Vr, "Safe Place VR", "Returned to a familiar forest scene.", 7, HeadsetTaylorId),
            Session(taylor, now.AddDays(-6), 32, TherapyPlatform.Vr, "Grounding Garden", "Steady breathing throughout.", 8, HeadsetTaylorId),
            Session(taylor, now.AddDays(-3), 30, TherapyPlatform.Vr, "Values Compass", "Identified top three values for treatment focus.", 8, HeadsetTaylorId),
            Session(taylor, now.AddDays(-1).AddHours(-5), 40, TherapyPlatform.Vr, "Grounding Garden", "Longest session this month; positive affect noted.", 8, HeadsetTaylorId),

            Session(casey, now.AddDays(-20), 21, TherapyPlatform.Mobile, "Gratitude Stack", "Listed weekday supports.", 7, MobileCaseyId),
            Session(casey, now.AddDays(-16), 22, TherapyPlatform.Vr, "Mandala Emotions", "Used warm colors; described the week as full.", 6, null),
            Session(casey, now.AddDays(-12), 18, TherapyPlatform.Mobile, "Mood Check-in", "Rated the week near usual.", 6, MobileCaseyId),
            Session(casey, now.AddDays(-9), 19, TherapyPlatform.Mobile, "Gratitude Stack", "Listed five supports; mood improved.", 6, MobileCaseyId),
            Session(casey, now.AddDays(-5), 32, TherapyPlatform.Vr, "Emotion River", "Tracked anger→sadness shift during module.", 4, null),
            Session(casey, now.AddDays(-2).AddHours(-1), 24, TherapyPlatform.Mobile, "Grief Narrative", "Wrote letter to lost family member.", 3, MobileCaseyId)
        };

        var devices = new List<CareDevice>
        {
            new() { Id = HeadsetTaylorId, DisplayName = "Reflect XR headset XR-204", Kind = DeviceKind.XrHeadset, HardwareId = "XR-204", CurrentPatientId = taylor },
            new() { Id = HeadsetSamId, DisplayName = "Reflect XR headset XR-118", Kind = DeviceKind.XrHeadset, HardwareId = "XR-118", CurrentPatientId = sam },
            new() { Id = HeadsetUnassignedId, DisplayName = "Reflect XR headset XR-090", Kind = DeviceKind.XrHeadset, HardwareId = "XR-090", CurrentPatientId = null },
            new() { Id = MobileJordanId, DisplayName = "Reflect Mobile · Jordan", Kind = DeviceKind.MobileApp, HardwareId = "MOB-5521", CurrentPatientId = jordan },
            new() { Id = MobileCaseyId, DisplayName = "Reflect Mobile · Casey", Kind = DeviceKind.MobileApp, HardwareId = "MOB-7740", CurrentPatientId = casey },
            new() { Id = MobileAlexId, DisplayName = "Reflect Mobile · Alex", Kind = DeviceKind.MobileApp, HardwareId = "MOB-3318", CurrentPatientId = alex }
        };

        var assignments = new List<DeviceAssignment>
        {
            Assignment(HeadsetTaylorId, taylor, now.AddDays(-40), null),
            Assignment(HeadsetSamId, sam, now.AddDays(-40), null),
            Assignment(HeadsetUnassignedId, alex, now.AddDays(-40), now.AddDays(-3)),
            Assignment(MobileJordanId, jordan, now.AddDays(-40), null),
            Assignment(MobileCaseyId, casey, now.AddDays(-40), null),
            Assignment(MobileAlexId, alex, now.AddDays(-40), null)
        };

        Patients = patients;
        Sessions = sessions;
        Devices = devices;
        DeviceAssignments = assignments;
    }

    private static TherapySession Session(
        Guid patientId,
        DateTimeOffset startedAtUtc,
        int durationMinutes,
        TherapyPlatform platform,
        string moduleName,
        string reflectionSummary,
        int affectScore,
        Guid? deviceId)
    {
        return new TherapySession
        {
            PatientId = patientId,
            StartedAtUtc = startedAtUtc.ToUniversalTime(),
            DurationMinutes = durationMinutes,
            Platform = platform,
            ModuleName = moduleName,
            ReflectionSummary = reflectionSummary,
            AffectScore = affectScore,
            DeviceId = deviceId
        };
    }

    private static DeviceAssignment Assignment(Guid deviceId, Guid? patientId, DateTimeOffset assignedAt, DateTimeOffset? unassignedAt)
    {
        return new DeviceAssignment
        {
            DeviceId = deviceId,
            PatientId = patientId,
            AssignedAtUtc = assignedAt.ToUniversalTime(),
            UnassignedAtUtc = unassignedAt?.ToUniversalTime()
        };
    }
}
