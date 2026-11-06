using System.Globalization;
using System.Text;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Reports;

namespace Reflect.ProviderDashboard.Web.Services;

public static class ClinicalReportPdf
{
    public static byte[] From(ClinicalReportViewModel report)
    {
        var pages = new List<string>();
        var y = 720;
        var stream = new StringBuilder();

        void Flush()
        {
            pages.Add(stream.ToString());
            stream.Clear();
            y = 720;
        }

        void Line(string font, int size, double x, string text)
        {
            if (y < 72)
                Flush();
            stream.Append("BT /").Append(font).Append(' ').Append(size)
                .Append(" Tf 1 0 0 1 ").Append(x.ToString("0.##", CultureInfo.InvariantCulture))
                .Append(' ').Append(y.ToString("0.##", CultureInfo.InvariantCulture))
                .Append(" Tm (").Append(Escape(text)).Append(") Tj ET\n");
            y -= size + 6;
        }

        Line("F2", 18, 48, report.PatientName);
        Line("F1", 10, 48, "Observation snapshot  ·  Reflect XR  ·  Persistent Technology");
        y -= 4;
        Line("F1", 9, 48, report.ExternalPatientId + "  ·  Prepared " + report.PreparedOn + "  ·  Synthetic demo");
        y -= 10;
        Line("F1", 11, 48, "Window: " + report.ReportingPeriod);
        Line("F2", 12, 48, "Main module: " + report.MainModule);
        Line("F1", 11, 48, "Prepared by: " + report.ClinicianName);

        if (report.IncludeSummary)
        {
            y -= 8;
            Line("F2", 12, 48, "Patient summary");
            foreach (var wrap in Wrap(report.NotesSummary, 92))
                Line("F1", 10, 48, wrap);
        }

        if (report.IncludeSplit)
        {
            y -= 8;
            Line("F2", 12, 48, "Activity in range");
            Line("F1", 11, 48, "Recorded sessions: " + report.SessionCount);
            Line("F1", 11, 48, "Engagement time: " + report.TotalMinutes + " min");
            Line("F1", 11, 48, "Platform split: " + report.PlatformSplitLabel);
        }

        if (report.IncludeHistory)
        {
            y -= 8;
            Line("F2", 12, 48, "Session engagement history");
            if (report.Sessions.Count == 0)
            {
                Line("F1", 10, 48, "No sessions in this reporting period.");
            }
            else
            {
                Line("F2", 9, 48, Pad("Date", 14) + Pad("Platform", 18) + Pad("Module", 28) + "Duration");
                foreach (var session in report.Sessions)
                {
                    Line("F1", 9, 48, Pad(session.Date, 14) + Pad(session.Platform, 18)
                        + Pad(session.Module, 28) + session.DurationMinutes + " min");
                }
            }
        }

        y -= 16;
        Line("F1", 8, 48, "Fictional records. This demo is not a HIPAA-covered production system.");
        Flush();
        return Assemble(pages);
    }

    private static string Pad(string value, int width)
    {
        value ??= string.Empty;
        if (value.Length >= width)
            return value[..Math.Max(1, width - 1)] + " ";
        return value + new string(' ', width - value.Length);
    }

    private static IEnumerable<string> Wrap(string value, int width)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield return "No clinical notes recorded.";
            yield break;
        }

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = new StringBuilder();
        foreach (var word in words)
        {
            if (line.Length + word.Length + 1 > width && line.Length > 0)
            {
                yield return line.ToString();
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0)
            yield return line.ToString();
    }

    private static string Escape(string value)
    {
        var ascii = new StringBuilder(value.Length);
        foreach (var ch in value ?? string.Empty)
        {
            ascii.Append(ch switch
            {
                '(' => @"\(",
                ')' => @"\)",
                '\\' => @"\\",
                '\n' or '\r' or '\t' => ' ',
                < ' ' or > '~' => '?',
                _ => ch
            });
        }
        return ascii.ToString();
    }

    private static byte[] Assemble(IReadOnlyList<string> contentStreams)
    {
        var latin1 = Encoding.Latin1;
        var objects = new List<byte[]>();
        objects.Add(latin1.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));

        var kids = new StringBuilder("<< /Type /Pages /Count ");
        kids.Append(contentStreams.Count).Append(" /Kids [");
        for (var i = 0; i < contentStreams.Count; i++)
            kids.Append(3 + i).Append(" 0 R ");
        kids.Append("] >>");
        objects.Add(latin1.GetBytes(kids.ToString()));

        var fontObject = 3 + contentStreams.Count * 2;
        for (var i = 0; i < contentStreams.Count; i++)
        {
            var contentId = 3 + contentStreams.Count + i;
            var page = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 "
                + fontObject + " 0 R /F2 " + (fontObject + 1) + " 0 R >> >> /Contents " + contentId + " 0 R >>";
            objects.Add(latin1.GetBytes(page));
        }

        foreach (var content in contentStreams)
        {
            var body = latin1.GetBytes(content);
            var stream = latin1.GetBytes("<< /Length " + body.Length + " >>\nstream\n")
                .Concat(body)
                .Concat(latin1.GetBytes("\nendstream"))
                .ToArray();
            objects.Add(stream);
        }

        objects.Add(latin1.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));
        objects.Add(latin1.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"));

        using var output = new MemoryStream();
        output.Write(latin1.GetBytes("%PDF-1.4\n"));
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            output.Write(latin1.GetBytes(i + 1 + " 0 obj\n"));
            output.Write(objects[i]);
            output.Write(latin1.GetBytes("\nendobj\n"));
        }

        var xref = output.Position;
        var xrefTable = new StringBuilder();
        xrefTable.Append("xref\n0 ").Append(objects.Count + 1).Append('\n');
        xrefTable.Append("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
            xrefTable.Append(offsets[i].ToString("0000000000", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        xrefTable.Append("trailer << /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
        output.Write(latin1.GetBytes(xrefTable.ToString()));
        return output.ToArray();
    }
}
