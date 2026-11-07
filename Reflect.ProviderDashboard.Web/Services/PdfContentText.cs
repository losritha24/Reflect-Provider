using System.IO.Compression;
using System.Text;

namespace Reflect.ProviderDashboard.Web.Services;

/// <summary>
/// Pulls visible text from a digital PDF (literal strings in content streams).
/// Does not OCR scans. Handles uncompressed and FlateDecode streams.
/// </summary>
internal static class PdfContentText
{
    public static string Read(ReadOnlySpan<byte> pdf)
    {
        if (pdf.Length < 5 || pdf[0] != (byte)'%' || pdf[1] != (byte)'P' || pdf[2] != (byte)'D' || pdf[3] != (byte)'F')
            throw new InvalidDataException("The file is not a PDF.");

        var source = Encoding.Latin1.GetString(pdf);
        var text = new StringBuilder();
        var searchFrom = 0;
        while (true)
        {
            var streamKeyword = IndexOfToken(source, "stream", searchFrom);
            if (streamKeyword < 0)
                break;
            var dictStart = source.LastIndexOf("<<", streamKeyword, streamKeyword);
            var dict = dictStart >= 0 ? source[dictStart..streamKeyword] : string.Empty;
            var dataStart = SkipStreamNewline(source, streamKeyword + 6);
            var endstream = IndexOfToken(source, "endstream", dataStart);
            if (endstream < 0)
                break;
            searchFrom = endstream + 9;
            var payload = Encoding.Latin1.GetBytes(source[dataStart..endstream]);
            if (dict.Contains("/FlateDecode", StringComparison.Ordinal))
                payload = Inflate(payload);
            AppendOperators(payload, text);
            text.Append('\n');
        }

        return text.ToString();
    }

    private static int IndexOfToken(string source, string token, int start)
    {
        var at = start;
        while (at >= 0 && at < source.Length)
        {
            at = source.IndexOf(token, at, StringComparison.Ordinal);
            if (at < 0)
                return -1;
            var before = at == 0 || !char.IsLetter(source[at - 1]);
            var afterIndex = at + token.Length;
            var after = afterIndex >= source.Length || !char.IsLetter(source[afterIndex]);
            if (before && after)
                return at;
            at += token.Length;
        }

        return -1;
    }

    private static int SkipStreamNewline(string source, int index)
    {
        if (index < source.Length && source[index] == '\r')
            index++;
        if (index < source.Length && source[index] == '\n')
            index++;
        return index;
    }

    private static byte[] Inflate(byte[] payload)
    {
        try
        {
            using var input = new MemoryStream(payload);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            return output.ToArray();
        }
        catch (InvalidDataException)
        {
            using var input = new MemoryStream(payload);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);
            return output.ToArray();
        }
    }

    private static void AppendOperators(byte[] content, StringBuilder text)
    {
        var source = Encoding.Latin1.GetString(content);
        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];
            if (c == '(')
            {
                i = ReadLiteral(source, i + 1, text);
                continue;
            }

            if (c is 'T' && i + 1 < source.Length && source[i + 1] is 'd' or 'D' or '*')
            {
                text.Append('\n');
                i++;
            }
        }
    }

    private static int ReadLiteral(string source, int start, StringBuilder text)
    {
        var i = start;
        while (i < source.Length)
        {
            var c = source[i];
            if (c == ')')
                return i;
            if (c == '\\' && i + 1 < source.Length)
            {
                var next = source[++i];
                text.Append(next switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => next
                });
            }
            else
            {
                text.Append(c);
            }

            i++;
        }

        return source.Length - 1;
    }
}
