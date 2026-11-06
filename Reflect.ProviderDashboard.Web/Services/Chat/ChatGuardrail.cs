using System.Text.RegularExpressions;

namespace Reflect.ProviderDashboard.Web.Services.Chat;

public sealed record GuardrailResult(bool Allowed, string Message, string Sanitized, IReadOnlyList<string> Redactions);

/// <summary>
/// Runs on every chat message before it reaches any responder (stub or, later, a real model).
/// Redacts identifiers, refuses obvious prompt-injection attempts and caps length.
/// Pattern matching is a first layer only; it is not a substitute for a BAA-covered model
/// vendor, zero-retention settings and output filtering when a real model is connected.
/// </summary>
public static class ChatGuardrail
{
    public const int MaxLength = 500;

    private static readonly (string Label, Regex Pattern)[] Redactors =
    [
        ("SSN", new Regex(@"\b\d{3}-?\d{2}-?\d{4}\b", RegexOptions.Compiled)),
        ("email", new Regex(@"\b[\w.+-]+@[\w-]+(\.[\w-]+)+\b", RegexOptions.Compiled)),
        ("phone", new Regex(@"\b(\+?1[\s.-]?)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}\b", RegexOptions.Compiled)),
        ("date", new Regex(@"\b(\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|\d{4}-\d{2}-\d{2})\b", RegexOptions.Compiled)),
        ("record-number", new Regex(@"\b(MRN|ID|patient)\s*#?:?\s*\d{4,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
    ];

    private static readonly Regex Injection = new(
        @"ignore (all |any )?(previous|prior|above) (instructions|rules)|disregard (the |your )?(system|previous)|reveal (your |the )?(system )?prompt|you are now|act as (an? )?(admin|developer|dan)|jailbreak|developer mode",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static GuardrailResult Inspect(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new(false, "Please enter a question.", "", []);
        if (input.Length > MaxLength)
            return new(false, $"Please keep questions under {MaxLength} characters.", "", []);
        if (Injection.IsMatch(input))
            return new(false, "I can't follow instructions that change how I work. Ask me about using the dashboard.", "", []);

        var redactions = new List<string>();
        var sanitized = input;
        foreach (var (label, pattern) in Redactors)
        {
            if (!pattern.IsMatch(sanitized)) continue;
            redactions.Add(label);
            sanitized = pattern.Replace(sanitized, $"[{label} removed]");
        }
        return new(true, "", sanitized, redactions);
    }
}
