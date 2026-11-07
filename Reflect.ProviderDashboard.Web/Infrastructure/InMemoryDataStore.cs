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
    public static readonly Guid HeadsetPriyaId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd7");
    public static readonly Guid MobileMarcusId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd8");
    public static readonly Guid HeadsetElenaId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd9");
    public static readonly Guid MobileDevonId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddda0");
    public static readonly Guid MobileHarperId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddda1");

    public static void AssignPatientsToProvider(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("A provider ID is required.", nameof(providerId));

        foreach (var patient in Patients)
            patient.AssignedProviderId = providerId;
    }

    public static void AddPatient(Patient patient)
    {
        ArgumentNullException.ThrowIfNull(patient);
        if (Patients is List<Patient> list)
            list.Add(patient);
        else
            Patients = Patients.Append(patient).ToList();
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
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"),
                DisplayName = "Priya Shah",
                ExternalPatientId = "PT-10096",
                DateOfBirth = new DateOnly(1985, 4, 21),
                Trend = EngagementTrend.Improving,
                NotesSummary = "Using Hope Tree and Future Self in XR to name supports for the next month."
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7"),
                DisplayName = "Marcus Webb",
                ExternalPatientId = "PT-10107",
                DateOfBirth = new DateOnly(1990, 8, 3),
                Trend = EngagementTrend.Stable,
                NotesSummary = "Prefers short mobile art sessions: Photo Coloring, Creativity, and Floating Bubbles."
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8"),
                DisplayName = "Elena Rossi",
                ExternalPatientId = "PT-10118",
                DateOfBirth = new DateOnly(1972, 12, 14),
                Trend = EngagementTrend.Stable,
                NotesSummary = "Regular XR practice with Self Compassion and Finding Balance after morning walks."
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9"),
                DisplayName = "Devon Okonkwo",
                ExternalPatientId = "PT-10129",
                DateOfBirth = new DateOnly(1998, 2, 9),
                Trend = EngagementTrend.Improving,
                NotesSummary = "Mixing Pour Paint on mobile with New Pathways in XR; engagement is picking up."
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10"),
                DisplayName = "Harper Lin",
                ExternalPatientId = "PT-10140",
                DateOfBirth = new DateOnly(2003, 6, 27),
                Trend = EngagementTrend.Stable,
                NotesSummary = "Consistent evening mobile use of Self and the prompt pathways."
            }
        };

        var now = DateTimeOffset.UtcNow;
        SeededAtUtc = now;
        var alex = patients[0].Id;
        var jordan = patients[1].Id;
        var sam = patients[2].Id;
        var taylor = patients[3].Id;
        var casey = patients[4].Id;
        var priya = patients[5].Id;
        var marcus = patients[6].Id;
        var elena = patients[7].Id;
        var devon = patients[8].Id;
        var harper = patients[9].Id;

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
            Session(casey, now.AddDays(-2).AddHours(-1), 24, TherapyPlatform.Mobile, ReflectModules.Mobile.Journey, "Wrote a letter to a lost family member in Journey.", 3, MobileCaseyId),

            Session(priya, now.AddDays(-11), 30, TherapyPlatform.Vr, ReflectModules.Xr.HopeTree, "Named three supports on the Hope Tree.", 6, HeadsetPriyaId),
            Session(priya, now.AddDays(-6), 28, TherapyPlatform.Vr, ReflectModules.Xr.FutureSelf, "Spoke with a calmer future self about work strain.", 7, HeadsetPriyaId),
            Session(priya, now.AddDays(-2), 32, TherapyPlatform.Vr, ReflectModules.Xr.AppreciationOfLife, "Listed small daily goods in Appreciation of Life.", 7, HeadsetPriyaId),

            Session(marcus, now.AddDays(-13), 16, TherapyPlatform.Mobile, ReflectModules.Mobile.PhotoColoring, "Colored a kitchen photo after dinner.", 5, MobileMarcusId),
            Session(marcus, now.AddDays(-8), 18, TherapyPlatform.Mobile, ReflectModules.Mobile.Creativity, "Tried a short Creativity prompt on the commute.", 6, MobileMarcusId),
            Session(marcus, now.AddDays(-3), 14, TherapyPlatform.Mobile, ReflectModules.Mobile.FloatingBubbles, "Used Floating Bubbles to settle before sleep.", 6, MobileMarcusId),
            Session(marcus, now.AddDays(-1), 17, TherapyPlatform.Mobile, ReflectModules.Mobile.PhotoColoring, "Returned to Photo Coloring; mood near usual.", 6, MobileMarcusId),

            Session(elena, now.AddDays(-15), 26, TherapyPlatform.Vr, ReflectModules.Xr.SelfCompassion, "Completed Self Compassion without rushing.", 6, HeadsetElenaId),
            Session(elena, now.AddDays(-9), 24, TherapyPlatform.Vr, ReflectModules.Xr.FindingBalance, "Practiced Finding Balance after a morning walk.", 6, HeadsetElenaId),
            Session(elena, now.AddDays(-4), 22, TherapyPlatform.Vr, ReflectModules.Xr.PresentSelfInside, "Checked in with Present Self Inside.", 6, HeadsetElenaId),
            Session(elena, now.AddDays(-1), 25, TherapyPlatform.Vr, ReflectModules.Xr.SelfCompassion, "Repeated Self Compassion; affect stayed even.", 6, HeadsetElenaId),

            Session(devon, now.AddDays(-10), 20, TherapyPlatform.Mobile, ReflectModules.Mobile.PourPaint, "Poured cool colors after a long shift.", 5, MobileDevonId),
            Session(devon, now.AddDays(-7), 34, TherapyPlatform.Vr, ReflectModules.Xr.NewPathways, "Tried New Pathways for the first time this month.", 6, null),
            Session(devon, now.AddDays(-3), 16, TherapyPlatform.Mobile, ReflectModules.Mobile.Emotions, "Labeled the week in Emotions.", 6, MobileDevonId),
            Session(devon, now.AddDays(-1), 19, TherapyPlatform.Mobile, ReflectModules.Mobile.PourPaint, "Longer Pour Paint session; reported more ease.", 7, MobileDevonId),

            Session(harper, now.AddDays(-14), 15, TherapyPlatform.Mobile, ReflectModules.Mobile.Self, "Evening Self check-in after class.", 5, MobileHarperId),
            Session(harper, now.AddDays(-8), 18, TherapyPlatform.Mobile, ReflectModules.Mobile.OptionsPromptPathway, "Chose a prompt in Options Prompt Pathway.", 6, MobileHarperId),
            Session(harper, now.AddDays(-4), 16, TherapyPlatform.Mobile, ReflectModules.Mobile.SelectFromImagesPathway, "Picked images for the week in Select from Images Pathway.", 6, MobileHarperId),
            Session(harper, now.AddDays(-1), 14, TherapyPlatform.Mobile, ReflectModules.Mobile.Self, "Short Self session before bed.", 6, MobileHarperId)
        };

        var devices = new List<CareDevice>
        {
            new() { Id = HeadsetTaylorId, DisplayName = "Reflect XR headset XR-204", Kind = DeviceKind.XrHeadset, HardwareId = "XR-204", CurrentPatientId = taylor },
            new() { Id = HeadsetSamId, DisplayName = "Reflect XR headset XR-118", Kind = DeviceKind.XrHeadset, HardwareId = "XR-118", CurrentPatientId = sam },
            new() { Id = HeadsetUnassignedId, DisplayName = "Reflect XR headset XR-090", Kind = DeviceKind.XrHeadset, HardwareId = "XR-090", CurrentPatientId = null },
            new() { Id = MobileJordanId, DisplayName = "Reflect Mobile · Jordan", Kind = DeviceKind.MobileApp, HardwareId = "MOB-5521", CurrentPatientId = jordan },
            new() { Id = MobileCaseyId, DisplayName = "Reflect Mobile · Casey", Kind = DeviceKind.MobileApp, HardwareId = "MOB-7740", CurrentPatientId = casey },
            new() { Id = MobileAlexId, DisplayName = "Reflect Mobile · Alex", Kind = DeviceKind.MobileApp, HardwareId = "MOB-3318", CurrentPatientId = alex },
            new() { Id = HeadsetPriyaId, DisplayName = "Reflect XR headset XR-221", Kind = DeviceKind.XrHeadset, HardwareId = "XR-221", CurrentPatientId = priya },
            new() { Id = MobileMarcusId, DisplayName = "Reflect Mobile · Marcus", Kind = DeviceKind.MobileApp, HardwareId = "MOB-4401", CurrentPatientId = marcus },
            new() { Id = HeadsetElenaId, DisplayName = "Reflect XR headset XR-307", Kind = DeviceKind.XrHeadset, HardwareId = "XR-307", CurrentPatientId = elena },
            new() { Id = MobileDevonId, DisplayName = "Reflect Mobile · Devon", Kind = DeviceKind.MobileApp, HardwareId = "MOB-6620", CurrentPatientId = devon },
            new() { Id = MobileHarperId, DisplayName = "Reflect Mobile · Harper", Kind = DeviceKind.MobileApp, HardwareId = "MOB-8814", CurrentPatientId = harper }
        };

        var assignments = new List<DeviceAssignment>
        {
            Assignment(HeadsetTaylorId, taylor, now.AddDays(-40), null),
            Assignment(HeadsetSamId, sam, now.AddDays(-40), null),
            Assignment(HeadsetUnassignedId, alex, now.AddDays(-40), now.AddDays(-3)),
            Assignment(MobileJordanId, jordan, now.AddDays(-40), null),
            Assignment(MobileCaseyId, casey, now.AddDays(-40), null),
            Assignment(MobileAlexId, alex, now.AddDays(-40), null),
            Assignment(HeadsetPriyaId, priya, now.AddDays(-40), null),
            Assignment(MobileMarcusId, marcus, now.AddDays(-40), null),
            Assignment(HeadsetElenaId, elena, now.AddDays(-40), null),
            Assignment(MobileDevonId, devon, now.AddDays(-40), null),
            Assignment(MobileHarperId, harper, now.AddDays(-40), null)
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
