using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;

namespace Reflect.ProviderDashboard.Web.Infrastructure;

public static class InMemoryDataStore
{
    public static IReadOnlyList<Patient> Patients { get; private set; } = new List<Patient>();
    public static IReadOnlyList<TherapySession> Sessions { get; private set; } = new List<TherapySession>();

    public static DateTimeOffset SeededAtUtc { get; private set; }

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
        var sessions = new List<TherapySession>
        {
            Session(patients[0].Id, now.AddDays(-1).AddHours(-2), 28, TherapyPlatform.Vr, "Mandala Emotions", "Named anxiety as a color wheel; chose cool tones for regulation."),
            Session(patients[0].Id, now.AddDays(-4), 22, TherapyPlatform.Mobile, "Guided Reflection", "Wrote about workplace stressors and coping plan."),
            Session(patients[0].Id, now.AddDays(-8), 35, TherapyPlatform.Vr, "Safe Place VR", "Built coastal scene; reported 6/10 calm post-session."),
            Session(patients[1].Id, now.AddDays(-2), 18, TherapyPlatform.Mobile, "Mood Check-in", "Mood 5→7 after gratitude prompt."),
            Session(patients[1].Id, now.AddDays(-6), 15, TherapyPlatform.Mobile, "AI Imagery Journal", "Generated image for 'hope'; short narrative attached."),
            Session(patients[2].Id, now.AddDays(-12), 20, TherapyPlatform.Mobile, "Sleep Wind-down", "Completed breathing; did not finish reflection."),
            Session(patients[2].Id, now.AddDays(-14), 25, TherapyPlatform.Vr, "Body Scan VR", "Ended session early due to discomfort."),
            Session(patients[3].Id, now.AddDays(-1).AddHours(-5), 40, TherapyPlatform.Vr, "Grounding Garden", "Longest session this month; positive affect noted."),
            Session(patients[3].Id, now.AddDays(-3), 30, TherapyPlatform.Vr, "Values Compass", "Identified top three values for treatment focus."),
            Session(patients[4].Id, now.AddDays(-2).AddHours(-1), 24, TherapyPlatform.Mobile, "Grief Narrative", "Wrote letter to lost family member."),
            Session(patients[4].Id, now.AddDays(-5), 32, TherapyPlatform.Vr, "Emotion River", "Tracked anger→sadness shift during module."),
            Session(patients[4].Id, now.AddDays(-9), 19, TherapyPlatform.Mobile, "Gratitude Stack", "Listed five supports; mood improved.")
        };

        Patients = patients;
        Sessions = sessions;
    }

    private static TherapySession Session(
        Guid patientId,
        DateTimeOffset startedAtUtc,
        int durationMinutes,
        TherapyPlatform platform,
        string moduleName,
        string reflectionSummary)
    {
        return new TherapySession
        {
            PatientId = patientId,
            StartedAtUtc = startedAtUtc,
            DurationMinutes = durationMinutes,
            Platform = platform,
            ModuleName = moduleName,
            ReflectionSummary = reflectionSummary
        };
    }
}
