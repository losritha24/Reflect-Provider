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
                NotesSummary = "Increased XR use; reports clearer emotional labeling after Color Psychology and Breathing Mandala."
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                DisplayName = "Jordan Kim",
                ExternalPatientId = "PT-10052",
                DateOfBirth = new DateOnly(1988, 7, 22),
                Trend = EngagementTrend.Stable,
                NotesSummary = "Steady Reflection Journals on mobile; prefers evening sessions."
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
                NotesSummary = "Strong engagement with Flow and Containment in Reflect XR."
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"),
                DisplayName = "Casey Nguyen",
                ExternalPatientId = "PT-10085",
                DateOfBirth = new DateOnly(1996, 9, 18),
                Trend = EngagementTrend.Stable,
                NotesSummary = "Balanced XR and mobile use; working through Journey and Catharsis."
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
            Session(alex, now.AddDays(-18), 20, TherapyPlatform.Mobile, ReflectModules.Mobile.BreathingMandala, "Completed a slow Breathing Mandala cycle; rated the week as manageable.", 5, MobileAlexId),
            Session(alex, now.AddDays(-12), 24, TherapyPlatform.Vr, ReflectModules.Xr.ColorPsychology, "Linked palette choices in Color Psychology to the week's mood.", 6, HeadsetUnassignedId),
            Session(alex, now.AddDays(-8), 35, TherapyPlatform.Vr, ReflectModules.Xr.Containment, "Finished Containment; reported 6/10 calm post-session.", 6, HeadsetUnassignedId),
            Session(alex, now.AddDays(-4), 22, TherapyPlatform.Mobile, ReflectModules.Mobile.ReflectionJournals, "Wrote about workplace stressors and a coping plan in Reflection Journals.", 7, MobileAlexId),
            Session(alex, now.AddDays(-1).AddHours(-2), 28, TherapyPlatform.Vr, ReflectModules.Xr.ColorPsychology, "Named anxiety as cool tones in Color Psychology.", 7, HeadsetUnassignedId),

            Session(jordan, now.AddDays(-16), 14, TherapyPlatform.Mobile, ReflectModules.Mobile.ReflectionJournals, "Evening Reflection Journals entry after work.", 5, MobileJordanId),
            Session(jordan, now.AddDays(-10), 16, TherapyPlatform.Mobile, ReflectModules.Mobile.BreathingMandala, "Used Breathing Mandala before sleep.", 5, MobileJordanId),
            Session(jordan, now.AddDays(-6), 15, TherapyPlatform.Mobile, ReflectModules.Mobile.CreateYourOwnPrompt, "Created an image for 'hope' in Create Your Own Prompt.", 6, MobileJordanId),
            Session(jordan, now.AddDays(-2), 18, TherapyPlatform.Mobile, ReflectModules.Mobile.ReflectionJournals, "Mood 5→7 after a gratitude prompt in Reflection Journals.", 7, MobileJordanId),

            Session(sam, now.AddDays(-28), 26, TherapyPlatform.Vr, ReflectModules.Xr.Containment, "Completed the full Containment scene.", 7, HeadsetSamId),
            Session(sam, now.AddDays(-24), 22, TherapyPlatform.Mobile, ReflectModules.Mobile.ReflectionJournals, "Described a typical week in Reflection Journals.", 7, null),
            Session(sam, now.AddDays(-20), 24, TherapyPlatform.Vr, ReflectModules.Xr.Flow, "Stayed with Flow through the exercise.", 6, HeadsetSamId),
            Session(sam, now.AddDays(-16), 18, TherapyPlatform.Mobile, ReflectModules.Mobile.FingerPainting, "Shorter Finger Painting session than usual.", 7, null),
            Session(sam, now.AddDays(-14), 25, TherapyPlatform.Vr, ReflectModules.Xr.RhythmicRelaxation, "Ended Rhythmic Relaxation early due to discomfort.", 5, HeadsetSamId),
            Session(sam, now.AddDays(-12), 20, TherapyPlatform.Mobile, ReflectModules.Mobile.BreathingMandala, "Started Breathing Mandala; did not finish the cycle.", 4, null),

            Session(taylor, now.AddDays(-10), 28, TherapyPlatform.Vr, ReflectModules.Xr.Containment, "Returned to a familiar forest scene in Containment.", 7, HeadsetTaylorId),
            Session(taylor, now.AddDays(-6), 32, TherapyPlatform.Vr, ReflectModules.Xr.Flow, "Steady breathing throughout Flow.", 8, HeadsetTaylorId),
            Session(taylor, now.AddDays(-3), 30, TherapyPlatform.Vr, ReflectModules.Xr.PersonalStrengths, "Named top strengths in Personal Strengths for treatment focus.", 8, HeadsetTaylorId),
            Session(taylor, now.AddDays(-1).AddHours(-5), 40, TherapyPlatform.Vr, ReflectModules.Xr.Flow, "Longest Flow session this month; positive affect noted.", 8, HeadsetTaylorId),

            Session(casey, now.AddDays(-20), 21, TherapyPlatform.Mobile, ReflectModules.Mobile.ReflectionJournals, "Listed weekday supports in Reflection Journals.", 7, MobileCaseyId),
            Session(casey, now.AddDays(-16), 22, TherapyPlatform.Vr, ReflectModules.Xr.EmotionalExpression, "Used warm tones in Emotional Expression; described the week as full.", 6, null),
            Session(casey, now.AddDays(-12), 18, TherapyPlatform.Mobile, ReflectModules.Mobile.BreathingMandala, "Completed Breathing Mandala; rated the week near usual.", 6, MobileCaseyId),
            Session(casey, now.AddDays(-9), 19, TherapyPlatform.Mobile, ReflectModules.Mobile.AiCreatedMandala, "Generated a mandala of five supports in AI Created Mandala.", 6, MobileCaseyId),
            Session(casey, now.AddDays(-5), 32, TherapyPlatform.Vr, ReflectModules.Xr.Catharsis, "Tracked anger→sadness shift during Catharsis.", 4, null),
            Session(casey, now.AddDays(-2).AddHours(-1), 24, TherapyPlatform.Mobile, ReflectModules.Mobile.Journey, "Wrote a letter to a lost family member in Journey.", 3, MobileCaseyId)
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
