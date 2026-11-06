namespace Reflect.ProviderDashboard.Web.Services.Chat;

/// <summary>Seam for the future model integration. Receives only guardrail-sanitized text.</summary>
public interface IChatResponder
{
    Task<string> RespondAsync(string sanitizedMessage, CancellationToken ct);
}

/// <summary>Placeholder: keyword help for navigating the dashboard. No network, no patient data.</summary>
public sealed class StubChatResponder : IChatResponder
{
    public Task<string> RespondAsync(string message, CancellationToken ct)
    {
        var text = message.ToLowerInvariant();
        var reply =
            text.Contains("engage") ? "Engagement trends compare recent sessions with earlier ones. Open Patients and use the engagement filter to narrow the list."
            : text.Contains("time") || text.Contains("zone") ? "Timestamps are stored in UTC and shown in Eastern Time for this demo."
            : text.Contains("patient") ? "Open Patients in the sidebar to search the caseload, then select a name for the session timeline."
            : text.Contains("session") ? "Sessions combine mobile and XR usage. The dashboard shows the last seven days by platform."
            : "This assistant is a prototype and is not connected to a language model yet. Try asking about patients, sessions, engagement or time zones.";
        return Task.FromResult(reply);
    }
}
