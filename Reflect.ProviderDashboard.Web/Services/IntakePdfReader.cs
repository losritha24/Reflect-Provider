using System.Globalization;
using System.Text.RegularExpressions;

namespace Reflect.ProviderDashboard.Web.Services;

public sealed class IntakeDraft
{
    public string DisplayName { get; init; } = string.Empty;
    public string ExternalPatientId { get; init; } = string.Empty;
    public DateOnly? DateOfBirth { get; init; }
    public string NotesSummary { get; init; } = string.Empty;

    public bool FoundAnything =>
        !string.IsNullOrWhiteSpace(DisplayName)
        || !string.IsNullOrWhiteSpace(ExternalPatientId)
        || DateOfBirth is not null
        || !string.IsNullOrWhiteSpace(NotesSummary);

    public string Notice
    {
        get
        {
            if (!FoundAnything)
                return "No labeled name, patient ID, or date of birth was found. You can still type the fields.";
            var filled = new List<string>();
            if (!string.IsNullOrWhiteSpace(DisplayName)) filled.Add("name");
            if (!string.IsNullOrWhiteSpace(ExternalPatientId)) filled.Add("patient ID");
            if (DateOfBirth is not null) filled.Add("date of birth");
            if (!string.IsNullOrWhiteSpace(NotesSummary)) filled.Add("notes");
            return "Filled " + string.Join(", ", filled) + " from the PDF. Check them before adding.";
        }
    }
}

public static class IntakePdfReader
{
    public const int MaxBytes = 1_048_576;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);
    private static readonly Regex Labeled = new(
        @"^(?<label>name|patient\s*name|full\s*name|patient\s*id|external\s*id|mrn|id|date\s*of\s*birth|dob|birth\s*date|notes|summary)\s*[:\-]\s*(?<value>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant,
        RegexTimeout);
    private static readonly Regex PatientIdToken = new(
        @"\bPT-[A-Za-z0-9]{3,}\b",
        RegexOptions.CultureInvariant,
        RegexTimeout);
    private static readonly Regex IsoDate = new(
        @"\b(19|20)\d{2}-\d{2}-\d{2}\b",
        RegexOptions.CultureInvariant,
        RegexTimeout);

    public static IntakeDraft Read(Stream pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        using var copy = new MemoryStream();
        pdf.CopyTo(copy);
        if (copy.Length == 0)
            throw new InvalidDataException("The PDF was empty.");
        if (copy.Length > MaxBytes)
            throw new InvalidDataException("Use a PDF smaller than 1 MB.");
        var bytes = copy.ToArray();
        var text = PdfContentText.Read(bytes);
        return Parse(text);
    }

    public static IntakeDraft Parse(string text)
    {
        var name = "";
        var externalId = "";
        DateOnly? dob = null;
        var notes = "";

        foreach (Match match in Labeled.Matches(text ?? string.Empty))
        {
            var label = Regex.Replace(match.Groups["label"].Value, @"\s+", " ").Trim().ToLowerInvariant();
            var value = match.Groups["value"].Value.Trim();
            if (string.IsNullOrWhiteSpace(value))
                continue;
            switch (label)
            {
                case "name":
                case "patient name":
                case "full name":
                    if (name.Length == 0) name = Truncate(value, 200);
                    break;
                case "patient id":
                case "external id":
                case "mrn":
                case "id":
                    if (externalId.Length == 0) externalId = Truncate(SanitizeId(value), 64);
                    break;
                case "date of birth":
                case "dob":
                case "birth date":
                    dob ??= TryDate(value);
                    break;
                case "notes":
                case "summary":
                    if (notes.Length == 0) notes = Truncate(value, 2000);
                    break;
            }
        }

        if (externalId.Length == 0)
        {
            var token = PatientIdToken.Match(text ?? string.Empty);
            if (token.Success)
                externalId = token.Value;
        }

        dob ??= TryDate(IsoDate.Match(text ?? string.Empty).Value);

        if (name.Length == 0)
            name = FirstPersonLine(text ?? string.Empty);

        return new IntakeDraft
        {
            DisplayName = name,
            ExternalPatientId = externalId,
            DateOfBirth = dob,
            NotesSummary = notes
        };
    }

    private static string FirstPersonLine(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length < 3 || line.Length > 80)
                continue;
            if (line.Contains(':') || line.Contains("Reflect", StringComparison.OrdinalIgnoreCase)
                || line.Contains("snapshot", StringComparison.OrdinalIgnoreCase)
                || line.Contains("PDF", StringComparison.OrdinalIgnoreCase))
                continue;
            var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length is >= 2 and <= 5 && words.All(word => word.All(ch => char.IsLetter(ch) || ch is '-' or '\'')))
                return Truncate(line, 200);
        }

        return string.Empty;
    }

    private static DateOnly? TryDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var slice = value.Trim();
        if (DateOnly.TryParse(slice, CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
            return iso;
        if (DateOnly.TryParse(slice, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var us))
            return us;
        return null;
    }

    private static string SanitizeId(string value)
    {
        var trimmed = value.Trim().Split(' ', 2)[0];
        return new string(trimmed.Where(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_').ToArray());
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
