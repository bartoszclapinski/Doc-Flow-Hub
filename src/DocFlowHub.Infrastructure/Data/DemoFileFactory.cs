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

    /// <summary>A line that starts a new page in a PDF. Other formats leave it out.</summary>
    public const string PageBreak = "\f";

    /// <summary>Prefix of a heading line: bold in a PDF, a "Heading 1" paragraph in a DOCX; TXT and MD keep the text without it.</summary>
    public const string HeadingPrefix = "# ";

    /// <summary>A bullet list item (Sprint 20 for DOCX; Sprint 24: every format).</summary>
    public const string BulletPrefix = "- ";

    /// <summary>
    /// A table row, its cells separated by <see cref="CellSeparator"/>. Rows that follow each other make
    /// one table; the first is its header row. DOCX and Markdown draw a real table, TXT lines the columns
    /// up, a PDF writes the cells side by side.
    /// </summary>
    public const string RowPrefix = "| ";
    public const string CellSeparator = " | ";

    /// <summary>Text between two of these is bold; a whole line between single <c>*</c> is italic (DOCX, Markdown).</summary>
    public const string BoldMark = "**";

    /// <summary>A line that opens or closes a code block (Sprint 24): Markdown keeps it, the others indent the code.</summary>
    public const string CodeFence = "```";

    /// <summary>Picture types of the demo (Sprint 24); they are drawn by <c>DemoImageFactory</c>, not built from lines.</summary>
    public static readonly string[] PictureTypes = { "png" };

    /// <summary>True for a type the demo can build a file for; accepts "pdf" or ".pdf".</summary>
    public static bool IsSupported(string? fileType) =>
        !string.IsNullOrEmpty(fileType) && (SupportedTypes.Contains(Normalize(fileType)) || PictureTypes.Contains(Normalize(fileType)));

    /// <summary>Builds the file; <paramref name="fileType"/> may be "pdf" or ".pdf".</summary>
    public static byte[] Create(string fileType, string title, IReadOnlyList<string> lines) =>
        Normalize(fileType) switch
        {
            "pdf" => CreatePdf(title, lines),
            "docx" => CreateDocx(title, lines),
            "md" => Encoding.UTF8.GetBytes(CreateMarkdown(title, lines)),
            "txt" => Encoding.UTF8.GetBytes(CreateText(title, lines)),
            _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, "Unsupported demo file type"),
        };

    /// <summary>A line without the bold and italic marks.</summary>
    private static string Unmark(string line)
    {
        var text = line.Replace(BoldMark, string.Empty);
        return text.Length > 2 && text[0] == '*' && text[^1] == '*' ? text[1..^1] : text;
    }

    private static string[] Cells(string row) => row[RowPrefix.Length..].Split(CellSeparator).Select(c => c.Trim()).ToArray();

    /// <summary>
    /// Markdown with real structure (Sprint 24): "## " headings, lists, pipe tables with a header row,
    /// fenced code. Blocks are separated by one blank line; list items and table rows stay together.
    /// </summary>
    private static string CreateMarkdown(string title, IReadOnlyList<string> lines)
    {
        var sb = new StringBuilder($"# {title}\n");
        var block = "start";
        var code = false;
        void Open(string kind)
        {
            if (block != kind || kind is "para" or "heading") sb.Append('\n');
            block = kind;
        }

        foreach (var line in lines)
        {
            if (line == PageBreak) continue;
            if (line == CodeFence)
            {
                if (!code) Open("code");
                sb.Append(CodeFence).Append('\n');
                code = !code;
                if (!code) block = "code-end";
                continue;
            }
            if (code)
            {
                sb.Append(line).Append('\n');
                continue;
            }
            if (line.Length == 0)
            {
                block = "gap";
                continue;
            }
            if (line.StartsWith(HeadingPrefix, StringComparison.Ordinal))
            {
                Open("heading");
                sb.Append("## ").Append(line[HeadingPrefix.Length..]).Append('\n');
            }
            else if (line.StartsWith(BulletPrefix, StringComparison.Ordinal))
            {
                Open("list");
                sb.Append(line).Append('\n');
            }
            else if (line.StartsWith(RowPrefix, StringComparison.Ordinal))
            {
                var cells = Cells(line);
                var first = block != "table";
                Open("table");
                sb.Append("| ").Append(string.Join(" | ", cells)).Append(" |\n");
                if (first) sb.Append('|').Append(string.Concat(cells.Select(_ => " --- |"))).Append('\n');
            }
            else
            {
                Open("para");
                sb.Append(line).Append('\n');
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Plain text (Sprint 24): underlined headings, "- " lists, tables with lined-up columns and indented
    /// code; the bold and italic marks are left out.
    /// </summary>
    private static string CreateText(string title, IReadOnlyList<string> lines)
    {
        var sb = new StringBuilder($"{title}\n{new string('=', title.Length)}\n\n");
        var code = false;
        var rows = new List<string[]>();
        void FlushTable()
        {
            if (rows.Count == 0) return;
            var widths = Enumerable.Range(0, rows.Max(r => r.Length))
                .Select(i => rows.Max(r => i < r.Length ? r[i].Length : 0)).ToArray();
            for (var r = 0; r < rows.Count; r++)
            {
                sb.Append(string.Join("  ", rows[r].Select((c, i) => c.PadRight(widths[i]))).TrimEnd()).Append('\n');
                if (r == 0) sb.Append(string.Join("  ", widths.Select(w => new string('-', w)))).Append('\n');
            }
            rows.Clear();
        }

        foreach (var line in lines)
        {
            if (line == PageBreak) continue;
            if (line == CodeFence)
            {
                FlushTable();
                code = !code;
                continue;
            }
            if (code)
            {
                sb.Append("    ").Append(line).Append('\n');
                continue;
            }
            if (line.StartsWith(RowPrefix, StringComparison.Ordinal))
            {
                rows.Add(Cells(line));
                continue;
            }
            FlushTable();
            if (line.StartsWith(HeadingPrefix, StringComparison.Ordinal))
            {
                var heading = Unmark(line[HeadingPrefix.Length..]);
                sb.Append('\n').Append(heading).Append('\n').Append(new string('-', heading.Length)).Append('\n');
            }
            else
            {
                sb.Append(Unmark(line)).Append('\n');
            }
        }
        FlushTable();
        return sb.ToString();
    }

    /// <summary>The lines as a PDF lays them out: no marks, table cells side by side, code indented.</summary>
    private static IEnumerable<string> PdfLines(IReadOnlyList<string> lines)
    {
        var code = false;
        foreach (var line in lines)
        {
            if (line == CodeFence)
            {
                code = !code;
                continue;
            }
            if (code) yield return "    " + line;
            else if (line.StartsWith(RowPrefix, StringComparison.Ordinal)) yield return string.Join("     ", Cells(line));
            else if (line == PageBreak || line.StartsWith(HeadingPrefix, StringComparison.Ordinal)) yield return line.Replace(BoldMark, string.Empty);
            else yield return Unmark(line);
        }
    }

    public static string GetContentType(string fileType) =>
        Normalize(fileType) switch
        {
            "pdf" => "application/pdf",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "md" => "text/markdown",
            "png" => "image/png",
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

    /// <summary>
    /// Writes a DOCX with real structure (Sprint 20), so the formatted preview has something to show: the
    /// title in the "Title" style, <see cref="HeadingPrefix"/> lines as "Heading 1", <see cref="BulletPrefix"/>
    /// lines as a bullet list, <see cref="RowPrefix"/> lines as a table, <see cref="BoldMark"/> for bold.
    /// </summary>
    private static byte[] CreateDocx(string title, IReadOnlyList<string> lines)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();

            main.AddNewPart<StyleDefinitionsPart>().Styles = new W.Styles(
                new W.Style(
                    new W.StyleName { Val = "Title" },
                    new W.StyleParagraphProperties(new W.SpacingBetweenLines { After = "240" }),
                    new W.StyleRunProperties(new W.Bold(), new W.FontSize { Val = "40" }))
                { Type = W.StyleValues.Paragraph, StyleId = "Title" },
                new W.Style(
                    new W.StyleName { Val = "heading 1" },
                    new W.StyleParagraphProperties(new W.KeepNext(), new W.SpacingBetweenLines { Before = "360", After = "120" }, new W.OutlineLevel { Val = 0 }),
                    new W.StyleRunProperties(new W.Bold(), new W.FontSize { Val = "30" }))
                { Type = W.StyleValues.Paragraph, StyleId = "Heading1" });

            main.AddNewPart<NumberingDefinitionsPart>().Numbering = new W.Numbering(
                new W.AbstractNum(
                    new W.Level(
                        new W.StartNumberingValue { Val = 1 },
                        new W.NumberingFormat { Val = W.NumberFormatValues.Bullet },
                        new W.LevelText { Val = "\u2022" },
                        new W.PreviousParagraphProperties(new W.Indentation { Left = "720", Hanging = "360" }))
                    { LevelIndex = 0 })
                { AbstractNumberId = 0 },
                new W.NumberingInstance(new W.AbstractNumId { Val = 0 }) { NumberID = 1 });

            var body = new W.Body();
            body.Append(new W.Paragraph(
                new W.ParagraphProperties(new W.ParagraphStyleId { Val = "Title" }),
                new W.Run(new W.Text(title))));

            W.Table? table = null;
            var code = false;
            foreach (var line in lines)
            {
                if (line == PageBreak) continue;
                if (line == CodeFence)
                {
                    code = !code;
                    continue;
                }
                if (code)
                {
                    body.Append(new W.Paragraph(new W.Run(
                        new W.RunProperties(new W.RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" }, new W.FontSize { Val = "19" }),
                        DocxText(line))));
                    continue;
                }

                if (line.StartsWith(RowPrefix, StringComparison.Ordinal))
                {
                    var header = table == null;
                    if (table == null)
                    {
                        table = new W.Table(new W.TableProperties(
                            new W.TableWidth { Width = "5000", Type = W.TableWidthUnitValues.Pct },
                            new W.TableBorders(
                                new W.TopBorder { Val = W.BorderValues.Single, Size = 4 },
                                new W.LeftBorder { Val = W.BorderValues.Single, Size = 4 },
                                new W.BottomBorder { Val = W.BorderValues.Single, Size = 4 },
                                new W.RightBorder { Val = W.BorderValues.Single, Size = 4 },
                                new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Size = 4 },
                                new W.InsideVerticalBorder { Val = W.BorderValues.Single, Size = 4 })));
                        body.Append(table);
                    }
                    var row = new W.TableRow();
                    if (header) row.Append(new W.TableRowProperties(new W.TableHeader()));
                    foreach (var cell in line[RowPrefix.Length..].Split(CellSeparator))
                    {
                        var run = new W.Run();
                        if (header) run.Append(new W.RunProperties(new W.Bold()));
                        run.Append(DocxText(cell.Trim()));
                        row.Append(new W.TableCell(new W.Paragraph(run)));
                    }
                    table.Append(row);
                    continue;
                }

                if (table != null)
                {
                    // Word needs a paragraph between a table and whatever follows it.
                    table = null;
                    if (line.Length == 0)
                    {
                        body.Append(new W.Paragraph());
                        continue;
                    }
                }

                if (line.StartsWith(HeadingPrefix, StringComparison.Ordinal))
                {
                    body.Append(new W.Paragraph(
                        new W.ParagraphProperties(new W.ParagraphStyleId { Val = "Heading1" }),
                        new W.Run(DocxText(line[HeadingPrefix.Length..]))));
                }
                else if (line.StartsWith(BulletPrefix, StringComparison.Ordinal))
                {
                    var item = new W.Paragraph(new W.ParagraphProperties(
                        new W.NumberingProperties(new W.NumberingLevelReference { Val = 0 }, new W.NumberingId { Val = 1 })));
                    AppendRuns(item, line[BulletPrefix.Length..]);
                    body.Append(item);
                }
                else
                {
                    var paragraph = new W.Paragraph();
                    AppendRuns(paragraph, line);
                    body.Append(paragraph);
                }
            }
            if (table != null) body.Append(new W.Paragraph());

            main.Document = new W.Document(body);
            main.Document.Save();
        }
        return ms.ToArray();
    }

    private static W.Text DocxText(string text) =>
        new(text) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve };

    /// <summary>Runs for one line: <c>*whole line*</c> is italic, text between <see cref="BoldMark"/> is bold.</summary>
    private static void AppendRuns(W.Paragraph paragraph, string line)
    {
        if (line.Length > 2 && line[0] == '*' && line[^1] == '*' && !line.StartsWith(BoldMark, StringComparison.Ordinal))
        {
            paragraph.Append(new W.Run(new W.RunProperties(new W.Italic()), DocxText(line[1..^1])));
            return;
        }

        var parts = line.Split(BoldMark);
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            var run = new W.Run();
            if (i % 2 == 1) run.Append(new W.RunProperties(new W.Bold()));
            run.Append(DocxText(parts[i]));
            paragraph.Append(run);
        }
    }

    /// <summary>
    /// Writes a minimal, valid PDF (Helvetica, one text line per entry) with a correctly computed
    /// xref table. Text is reduced to printable ASCII and escaped. Lines flow onto further Letter
    /// pages when a page is full; <see cref="PageBreak"/> starts a new page and a line starting with
    /// <see cref="HeadingPrefix"/> is a bold heading. Each page has "Page n of m" at the bottom.
    /// </summary>
    private static byte[] CreatePdf(string title, IReadOnlyList<string> lines)
    {
        // Lay the lines out on pages: the title takes the room of two lines on the first page.
        var pages = new List<List<(string Text, bool Heading)>> { new() };
        var used = 2;
        foreach (var raw in PdfLines(lines))
        {
            if (raw == PageBreak)
            {
                pages.Add(new());
                used = 0;
                continue;
            }
            var heading = raw.StartsWith(HeadingPrefix, StringComparison.Ordinal);
            var text = heading ? raw[HeadingPrefix.Length..] : raw;
            foreach (var line in Wrap(PdfText(text), MaxPdfLineChars))
            {
                if (used >= LinesPerPage)
                {
                    pages.Add(new());
                    used = 0;
                }
                pages[^1].Add((line, heading));
                used += heading ? 2 : 1;
            }
        }

        // Objects: 1 catalog, 2 page tree, 3 + 4 fonts, then a page and its content stream per page.
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            string.Empty, // the page tree, filled in below
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
        };
        var kids = new List<string>();
        for (var i = 0; i < pages.Count; i++)
        {
            var content = new StringBuilder("BT 72 740 Td");
            if (i == 0)
            {
                content.Append(" /F2 18 Tf 22 TL (").Append(PdfText(title)).Append(") Tj T* /F1 11 Tf 16 TL");
            }
            else
            {
                content.Append(" /F1 11 Tf 16 TL");
            }
            var first = true;
            foreach (var (text, heading) in pages[i])
            {
                if (!first) content.Append(" T*");
                if (heading)
                {
                    if (!first) content.Append(" T*");
                    content.Append(" /F2 13 Tf (").Append(text).Append(") Tj /F1 11 Tf");
                }
                else
                {
                    content.Append(" (").Append(text).Append(") Tj");
                }
                first = false;
            }
            content.Append($" ET BT /F1 9 Tf 280 40 Td (Page {i + 1} of {pages.Count}) Tj ET");
            var stream = content.ToString();

            var pageId = objects.Count + 1;
            kids.Add($"{pageId} 0 R");
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents {pageId + 1} 0 R /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> >>");
            objects.Add($"<< /Length {stream.Length} >>\nstream\n{stream}\nendstream");
        }
        objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", kids)}] /Count {pages.Count} >>";

        var sb = new StringBuilder("%PDF-1.4\n");
        var offsets = new int[objects.Count];
        for (var i = 0; i < objects.Count; i++)
        {
            offsets[i] = sb.Length;
            sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xrefStart = sb.Length;
        sb.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            sb.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }
        sb.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefStart}\n%%EOF");

        // Every char is ASCII here, so string length == byte offset.
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    // ~95 chars of 11pt Helvetica fit between the 72pt margins of a Letter page.
    private const int MaxPdfLineChars = 95;

    // 16pt lines from y = 740 down to the 72pt bottom margin (the page number sits below it).
    private const int LinesPerPage = 41;

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
