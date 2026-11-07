using Reflect.ProviderDashboard.Domain.Catalog;
using Reflect.ProviderDashboard.Domain.Enums;

namespace Reflect.ProviderDashboard.Web.Services.Chat;

/// <summary>Seam for the future model integration. Receives only guardrail-sanitized text.</summary>
public interface IChatResponder
{
    Task<string> RespondAsync(string sanitizedMessage, CancellationToken ct);
}

/// <summary>
/// Prepared answers about using the dashboard and about the Reflect module catalog.
/// No network, no patient data and no model; it matches keywords, so it is a help menu, not a conversation.
/// </summary>
public sealed class StubChatResponder : IChatResponder
{
    private static readonly (string[] Keys, string Reply)[] Topics =
    [
        (["crisis", "suicid", "self-harm", "self harm", "emergency", "harm myself", "kill myself"],
            "If someone may be in danger, contact local emergency services or call or text 988 (US) right away. This assistant can't help with clinical or crisis situations."),
        (["invite", "add a provider", "new provider", "add user", "team", "assign", "admin"],
            "Facility admins use Team in the sidebar: create an invite and copy the one-time link, then assign patients to the new provider. Admins see patient IDs only, not names or sessions."),
        (["message", "nudge", "notify", "push", "text a patient", "reach out"],
            "Open Messages (or Message this patient on a patient record) to queue a short nudge. It is a prototype: messages are queued here and not delivered, and no push notification is sent."),
        (["setting", "preference", "profile", "account", "theme", "dark mode", "text size", "font"],
            "Open Settings from the Account & display menu to change your profile, theme, text size, landing page and notification preferences. Accessibility options are in the same menu."),
        (["report", "print", "pdf", "export"],
            "Open Reports in the sidebar. Choose a patient and date range, then print the preview or download a PDF."),
        (["device", "headset", "install", "hardware"],
            "Devices lists headsets and mobile installs. You can move one to another patient without losing past sessions."),
        (["map", "graph", "network"],
            "Caseload map shows each person with their XR and mobile sessions, devices and follow-up signals. Thicker lines mean more sessions; coral marks a change from that person's usual pattern. A table version sits below it."),
        (["trust", "hipaa", "privacy", "security", "safeguard", "encrypt"],
            "Privacy & trust lists what protections exist today and what is still a production target. This demo uses fictional records only and is not a HIPAA-covered production system."),
        (["tour", "walkthrough", "getting started", "how do i start", "help"],
            "Choose Take the tour in the sidebar for a short walkthrough. Start with the Dashboard to see who needs follow-up, then open Patients for a person's timeline."),
        (["baseline", "below self", "affect", "trajectory", "mood"],
            "Affect markers are compared with each person's own rolling baseline, not a population norm. A coral flag means the latest score is below that person's usual range. It is a prompt to look, not a diagnosis."),
        (["attention", "quieter", "follow-up", "follow up", "queue"],
            "Needs attention lists people with fewer sessions this week than last week, with the counts shown so you can see why. Open the person to read their timeline."),
        (["engage", "trend"],
            "Engagement trends compare recent sessions with earlier ones. Open Patients and use the engagement filter to narrow the list."),
        (["time", "zone", "utc", "eastern"],
            "Timestamps are stored in UTC and shown in the facility time zone (Eastern in this demo), with EST or EDT shown."),
        (["module", "catalog", "program", "activity", "which modules"],
            "Module reference in the sidebar lists all 14 Reflect Mobile and 17 Reflect XR modules by program. Ask me about one by name, such as Hope Tree or Breathing Mandala."),
        (["login", "sign in", "sign out", "password", "log out", "logout"],
            "Sign out is in the Account & display menu. Accounts are invite-only: ask your facility admin for an invite if you need access."),
        (["patient", "caseload", "search", "filter"],
            "Open Patients in the sidebar to search the caseload and filter by engagement trend, then select a name for the session timeline."),
        (["session"],
            "Sessions combine mobile and XR usage. The dashboard shows the last seven days by platform, and each patient has a timeline."),
    ];

    public Task<string> RespondAsync(string message, CancellationToken ct)
    {
        var text = message.ToLowerInvariant();

        // Crisis wording wins over any other topic.
        var crisis = Topics[0];
        if (crisis.Keys.Any(text.Contains)) return Task.FromResult(crisis.Reply);

        var module = ReflectModuleCatalog.All
            .Where(m => text.Contains(m.Name.ToLowerInvariant()))
            .OrderByDescending(m => m.Name.Length).FirstOrDefault();
        if (module is not null) return Task.FromResult(DescribeModule(module));

        foreach (var (keys, reply) in Topics.Skip(1))
            if (keys.Any(text.Contains)) return Task.FromResult(reply);

        return Task.FromResult(
            "I can help you find your way around the dashboard. Try asking about patients, engagement, reports, messages, devices, the caseload map, settings, time zones, privacy, or a module such as Hope Tree. " +
            "I can't answer clinical questions or look at patient data.");
    }

    private static string DescribeModule(ReflectModule module)
    {
        var platform = module.Platform == TherapyPlatform.Vr ? "Reflect XR (VR on Meta Quest)" : "Reflect Mobile";
        var plan = module.MinimumPlan is { } p ? $" It is included from the {p} plan." : "";
        var ai = module.UsesAi ? " It uses AI image generation." : "";
        return $"{module.Name} is a {module.Group} module in {platform}.{plan}{ai} Sessions in it appear on a patient's timeline under {module.Group} · {module.Name}. See Module reference for the full list.";
    }
}
