using System.Security.Cryptography;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace DocFlowHub.Infrastructure.Data;

/// <summary>
/// Builds small but real demo files (PDF, DOCX, TXT, MD) for the seeded demo documents,
/// so downloads work and the files pass the same extension + signature checks as user
/// uploads. Content depends only on the text passed in, so a file can be rebuilt later
/// from the stored document metadata.
/// </summary>
public static class DemoFileFactory
{
    /// <summary>File types the demo seed uses — all are accepted by the upload whitelist.</summary>
    public static readonly string[] SupportedTypes = { "pdf", "docx", "txt", "md" };

    /// <summary>True for a type the factory can build; accepts "pdf" or ".pdf".</summary>
    public static bool IsSupported(string? fileType) =>
        !string.IsNullOrEmpty(fileType) && SupportedTypes.Contains(Normalize(fileType));

    /// <summary>Builds the file; <paramref name="fileType"/> may be "pdf" or ".pdf".</summary>
    public static byte[] Create(string fileType, string title, IReadOnlyList<string> lines) =>
        Normalize(fileType) switch
        {
            "pdf" => CreatePdf(title, lines),
            "docx" => CreateDocx(title, lines),
            "md" => Encoding.UTF8.GetBytes($"# {title}\n\n{string.Join("\n", lines)}\n"),
            "txt" => Encoding.UTF8.GetBytes($"{title}\n{new string('=', title.Length)}\n\n{string.Join("\n", lines)}\n"),
            _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, "Unsupported demo file type"),
        };

    public static string GetContentType(string fileType) =>
        Normalize(fileType) switch
        {
            "pdf" => "application/pdf",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "md" => "text/markdown",
            _ => "text/plain",
        };

    /// <summary>Same format as <c>DocumentStorageService.GetFileHashAsync</c> (MD5, upper hex).</summary>
    public static string ComputeHash(byte[] content) => Convert.ToHexString(MD5.HashData(content));

    public static IFormFile ToFormFile(byte[] content, string fileName, string fileType) =>
        new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = GetContentType(fileType),
        };

    private static string Normalize(string fileType) => fileType.TrimStart('.').ToLowerInvariant();

    private static byte[] CreateDocx(string title, IReadOnlyList<string> lines)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            var body = new W.Body();
            body.Append(new W.Paragraph(new W.Run(new W.RunProperties(new W.Bold(), new W.FontSize { Val = "32" }), new W.Text(title))));
            foreach (var line in lines)
            {
                body.Append(new W.Paragraph(new W.Run(new W.Text(line) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve })));
            }
            main.Document = new W.Document(body);
            main.Document.Save();
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a minimal, valid single-page PDF (Helvetica, one text line per entry) with a
    /// correctly computed xref table. Text is reduced to printable ASCII and escaped.
    /// </summary>
    private static byte[] CreatePdf(string title, IReadOnlyList<string> lines)
    {
        var content = new StringBuilder();
        content.Append("BT /F1 18 Tf 72 740 Td 22 TL (").Append(PdfText(title)).Append(") Tj /F1 11 Tf 16 TL T*");
        foreach (var line in lines.SelectMany(l => Wrap(PdfText(l), MaxPdfLineChars)))
        {
            content.Append(" T* (").Append(line).Append(") Tj");
        }
        content.Append(" ET");
        var stream = content.ToString();

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {stream.Length} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };

        var sb = new StringBuilder("%PDF-1.4\n");
        var offsets = new int[objects.Length];
        for (var i = 0; i < objects.Length; i++)
        {
            offsets[i] = sb.Length;
            sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xrefStart = sb.Length;
        sb.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            sb.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }
        sb.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefStart}\n%%EOF");

        // Every char is ASCII here, so string length == byte offset.
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    // ~95 chars of 11pt Helvetica fit between the 72pt margins of a Letter page.
    private const int MaxPdfLineChars = 95;

    /// <summary>Word-wraps an already-escaped line; never splits an escape sequence.</summary>
    private static IEnumerable<string> Wrap(string line, int max)
    {
        if (line.Length <= max)
        {
            yield return line;
            yield break;
        }

        var current = new StringBuilder();
        foreach (var word in line.Split(' '))
        {
            if (current.Length > 0 && current.Length + 1 + word.Length > max)
            {
                yield return current.ToString();
                current.Clear();
            }
            if (current.Length > 0) current.Append(' ');
            current.Append(word);
        }
        if (current.Length > 0) yield return current.ToString();
    }

    private static string PdfText(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            var c = ch switch
            {
                '—' or '–' => '-',
                '•' => '*',
                _ => ch,
            };
            if (c < 0x20 || c > 0x7E) continue; // drop anything Helvetica/ASCII can't show
            if (c is '(' or ')' or '\\') sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
