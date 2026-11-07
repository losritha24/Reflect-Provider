using System.Globalization;
using System.Text;

namespace Reflect.ProviderDashboard.Web.Services;

public static class IntakeLabeledPdf
{
    public static byte[] Create(string name, string externalId, string dateOfBirth, string notes)
    {
        var content = "BT /F1 12 Tf 72 720 Td (Name: " + Escape(name) + ") Tj 0 -18 Td (Patient ID: "
            + Escape(externalId) + ") Tj 0 -18 Td (Date of birth: " + Escape(dateOfBirth)
            + ") Tj 0 -18 Td (Notes: " + Escape(notes) + ") Tj ET\n";
        var latin1 = Encoding.Latin1;
        var body = latin1.GetBytes(content);
        var objects = new[]
        {
            latin1.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
            latin1.GetBytes("<< /Type /Pages /Count 1 /Kids [3 0 R] >>"),
            latin1.GetBytes("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>"),
            latin1.GetBytes("<< /Length " + body.Length + " >>\nstream\n").Concat(body).Concat(latin1.GetBytes("\nendstream")).ToArray(),
            latin1.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>")
        };

        using var output = new MemoryStream();
        output.Write(latin1.GetBytes("%PDF-1.4\n"));
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(output.Position);
            output.Write(latin1.GetBytes(i + 1 + " 0 obj\n"));
            output.Write(objects[i]);
            output.Write(latin1.GetBytes("\nendobj\n"));
        }

        var xref = output.Position;
        var table = new StringBuilder("xref\n0 ").Append(objects.Length + 1).Append('\n');
        table.Append("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
            table.Append(offsets[i].ToString("0000000000", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        table.Append("trailer << /Size ").Append(objects.Length + 1).Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xref).Append("\n%%EOF\n");
        output.Write(latin1.GetBytes(table.ToString()));
        return output.ToArray();
    }

    private static string Escape(string value) =>
        (value ?? string.Empty).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);
}
