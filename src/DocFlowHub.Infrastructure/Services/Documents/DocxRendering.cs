using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace DocFlowHub.Infrastructure.Services.Documents;

/// <summary>The formatted view of a DOCX: safe HTML, and whether the document was cut short.</summary>
public sealed record DocxHtml(string Html, bool Truncated);

/// <summary>
/// DOCX → HTML for the Details preview (Sprint 20). The writer emits a fixed set of tags and attributes
/// and HTML-encodes every piece of text, so nothing from the file reaches the page as markup. It shows
/// structure (headings, paragraphs, lists, tables, links, pictures), not the document's fonts or colours.
/// </summary>
public static class DocxRendering
{
    /// <summary>The HTML stops after this many characters (pictures not counted).</summary>
    public const int HtmlLimitChars = 1024 * 1024;

    /// <summary>All embedded pictures of one document may take this many bytes; the rest become placeholders.</summary>
    public const int PictureBudgetBytes = 1536 * 1024;

    /// <summary>Cap on one part of the package while it is read, so a zip bomb cannot exhaust memory.</summary>
    private const long MaxCharactersInPart = 20_000_000;

    private const int MaxDepth = 12;

    /// <summary>
    /// Renders the main document. Throws when the file cannot be opened as a DOCX (damaged,
    /// password-protected, a part over the size cap); the caller falls back to the text view.
    /// </summary>
    public static DocxHtml Render(byte[] content, int htmlLimit = HtmlLimitChars, int pictureBudget = PictureBudgetBytes)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var doc = WordprocessingDocument.Open(stream, false, new OpenSettings { MaxCharactersInPart = MaxCharactersInPart });
        var main = doc.MainDocumentPart;
        var body = main?.Document?.Body;
        if (main == null || body == null)
        {
            return new DocxHtml(string.Empty, false);
        }

        var writer = new Writer(main, htmlLimit, pictureBudget);
        writer.Blocks(body, depth: 0);
        return new DocxHtml(writer.Html, writer.Truncated);
    }

    [Flags]
    private enum Fmt { None = 0, Bold = 1, Italic = 2, Underline = 4, Strike = 8, Super = 16, Sub = 32 }

    private static readonly (Fmt Flag, string Tag)[] FmtTags =
    {
        (Fmt.Bold, "strong"), (Fmt.Italic, "em"), (Fmt.Underline, "u"), (Fmt.Strike, "s"), (Fmt.Super, "sup"), (Fmt.Sub, "sub"),
    };

    private static readonly Dictionary<string, string> PictureTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = "image/png",
        ["image/jpeg"] = "image/jpeg",
        ["image/jpg"] = "image/jpeg",
        ["image/gif"] = "image/gif",
    };

    private static readonly Regex HeadingName = new(@"^heading\s*([1-9])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>A run of inline HTML with one formatting; neighbours with the same formatting are joined.</summary>
    private sealed class Segment
    {
        public Fmt Format;
        public bool Atomic;   // a link: never joined with its neighbours
        public StringBuilder Html = new();
    }

    private sealed class ListState
    {
        public string Tag = "ul";
        public int NumId;
        public bool ItemOpen;
    }

    private sealed record ListInfo(int NumId, int Level, string Tag, string? Type, int Start);

    private sealed class Writer
    {
        private readonly MainDocumentPart _main;
        private readonly Dictionary<string, Style> _styles = new(StringComparer.OrdinalIgnoreCase);
        private readonly Numbering? _numbering;
        private readonly StringBuilder _html = new();
        private readonly int _htmlLimit;
        private int _pictureBudget;
        private int _pictureChars;
        private bool _pageBreak;

        public Writer(MainDocumentPart main, int htmlLimit, int pictureBudget)
        {
            _main = main;
            _htmlLimit = htmlLimit;
            _pictureBudget = pictureBudget;
            _numbering = main.NumberingDefinitionsPart?.Numbering;
            var styles = main.StyleDefinitionsPart?.Styles;
            if (styles != null)
            {
                foreach (var style in styles.Elements<Style>())
                {
                    var id = style.StyleId?.Value;
                    if (!string.IsNullOrEmpty(id)) _styles.TryAdd(id, style);
                }
            }
        }

        public string Html => _html.ToString();
        public bool Truncated { get; private set; }

        // ------------------------------------------------------------ blocks

        public void Blocks(OpenXmlElement container, int depth)
        {
            if (depth > MaxDepth) return;
            var lists = new Stack<ListState>();
            foreach (var block in Flatten(container))
            {
                if (block is Paragraph paragraph)
                {
                    WriteParagraph(paragraph, lists);
                }
                else if (block is Table table)
                {
                    CloseLists(lists);
                    WriteTable(table, depth);
                }

                if (depth == 0 && _html.Length - _pictureChars > _htmlLimit)
                {
                    Truncated = true;
                    break;
                }
            }
            CloseLists(lists);
        }

        /// <summary>Paragraphs and tables of a container, looking through content controls and custom XML.</summary>
        private static IEnumerable<OpenXmlElement> Flatten(OpenXmlElement container)
        {
            foreach (var child in container.ChildElements)
            {
                if (child is Paragraph or Table)
                {
                    yield return child;
                }
                else if (child is SdtBlock or SdtContentBlock or CustomXmlBlock)
                {
                    foreach (var inner in Flatten(child)) yield return inner;
                }
            }
        }

        private void WriteParagraph(Paragraph paragraph, Stack<ListState> lists)
        {
            var properties = paragraph.ParagraphProperties;
            var styleId = properties?.ParagraphStyleId?.Val?.Value;
            var (level, isTitle) = HeadingLevel(properties, styleId);

            _pageBreak = false;
            var segments = new List<Segment>();
            Inline(paragraph, segments, level == null ? styleId : null, 0);
            var inner = Join(segments);
            var breakAfter = _pageBreak;
            var breakBefore = On(properties?.PageBreakBefore);

            if (string.IsNullOrWhiteSpace(inner))
            {
                // An empty paragraph is only spacing in Word; a page break is still shown.
                if (breakBefore || breakAfter)
                {
                    CloseLists(lists);
                    PageBreak();
                }
                return;
            }

            var list = level == null ? ListOf(properties, styleId) : null;
            if (list != null)
            {
                ListItem(lists, list, inner);
                return;
            }

            CloseLists(lists);
            if (breakBefore) PageBreak();

            var tag = level == null ? "p" : "h" + level;
            var classes = new List<string>(2);
            if (isTitle) classes.Add("dfh-docx-title");
            var align = Alignment(properties, styleId);
            if (align != null) classes.Add(align);

            _html.Append('<').Append(tag);
            if (classes.Count > 0) _html.Append(" class=\"").Append(string.Join(' ', classes)).Append('"');
            _html.Append('>').Append(inner).Append("</").Append(tag).Append('>');

            if (breakAfter) PageBreak();
        }

        private void PageBreak() => _html.Append("<hr class=\"dfh-docx-break\">");

        /// <summary>
        /// The heading level (1–6) of a paragraph. Style ids depend on the language of the Word that wrote
        /// the file ("Heading1", "Nagwek1"), so the outline level and the style name decide (S20-3).
        /// </summary>
        private (int? Level, bool IsTitle) HeadingLevel(ParagraphProperties? properties, string? styleId)
        {
            var own = properties?.OutlineLevel?.Val?.Value;
            if (own is >= 0 and <= 8) return (Math.Min(own.Value + 1, 6), false);
            if (own != null) return (null, false);   // 9 = body text, set on the paragraph itself

            foreach (var style in Chain(styleId))
            {
                var name = style.StyleName?.Val?.Value?.Trim() ?? string.Empty;
                if (name.Equals("Title", StringComparison.OrdinalIgnoreCase)) return (1, true);
                var match = HeadingName.Match(name);
                if (match.Success) return (Math.Min(match.Groups[1].Value[0] - '0', 6), false);
                var outline = style.StyleParagraphProperties?.OutlineLevel?.Val?.Value;
                if (outline is >= 0 and <= 8) return (Math.Min(outline.Value + 1, 6), false);
            }

            // No styles part (or an unknown id): the English ids are still worth a try.
            if (styleId != null && !_styles.ContainsKey(styleId))
            {
                if (styleId.Equals("Title", StringComparison.OrdinalIgnoreCase)) return (1, true);
                var match = HeadingName.Match(styleId);
                if (match.Success) return (Math.Min(match.Groups[1].Value[0] - '0', 6), false);
            }
            return (null, false);
        }

        private string? Alignment(ParagraphProperties? properties, string? styleId)
        {
            var value = properties?.Justification?.Val;
            if (value == null)
            {
                foreach (var style in Chain(styleId))
                {
                    value = style.StyleParagraphProperties?.Justification?.Val;
                    if (value != null) break;
                }
            }
            if (value == null) return null;
            if (value.Value == JustificationValues.Center) return "is-center";
            if (value.Value == JustificationValues.Right || value.Value == JustificationValues.End) return "is-right";
            return null;
        }

        /// <summary>The style and the styles it is based on, nearest first.</summary>
        private IEnumerable<Style> Chain(string? styleId)
        {
            for (var i = 0; i < 12 && !string.IsNullOrEmpty(styleId); i++)
            {
                if (!_styles.TryGetValue(styleId, out var style)) yield break;
                yield return style;
                styleId = style.BasedOn?.Val?.Value;
            }
        }

        // ------------------------------------------------------------- lists

        private ListInfo? ListOf(ParagraphProperties? properties, string? styleId)
        {
            var numbering = properties?.NumberingProperties;
            if (numbering == null)
            {
                foreach (var style in Chain(styleId))
                {
                    numbering = style.StyleParagraphProperties?.NumberingProperties;
                    if (numbering != null) break;
                }
            }

            var numId = numbering?.NumberingId?.Val?.Value;
            if (numId is null or 0) return null;   // 0 turns numbering off
            var level = Math.Clamp(numbering!.NumberingLevelReference?.Val?.Value ?? 0, 0, 8);

            var definition = LevelOf(numId.Value, level);
            var format = definition?.NumberingFormat?.Val?.Value;
            string tag = "ul";
            string? type = null;
            if (format != null && format != NumberFormatValues.Bullet && format != NumberFormatValues.None)
            {
                tag = "ol";
                if (format == NumberFormatValues.LowerLetter) type = "a";
                else if (format == NumberFormatValues.UpperLetter) type = "A";
                else if (format == NumberFormatValues.LowerRoman) type = "i";
                else if (format == NumberFormatValues.UpperRoman) type = "I";
            }
            var start = tag == "ol" ? definition?.StartNumberingValue?.Val?.Value ?? 1 : 1;
            return new ListInfo(numId.Value, level, tag, type, start);
        }

        private Level? LevelOf(int numId, int level)
        {
            if (_numbering == null) return null;
            var instance = _numbering.Elements<NumberingInstance>().FirstOrDefault(n => n.NumberID?.Value == numId);
            var abstractId = instance?.AbstractNumId?.Val?.Value;
            if (abstractId == null) return null;
            var definition = _numbering.Elements<AbstractNum>().FirstOrDefault(a => a.AbstractNumberId?.Value == abstractId);
            return definition?.Elements<Level>().FirstOrDefault(l => l.LevelIndex?.Value == level);
        }

        private void ListItem(Stack<ListState> lists, ListInfo item, string inner)
        {
            var target = item.Level + 1;
            while (lists.Count > target) CloseList(lists);

            if (lists.Count == target)
            {
                var top = lists.Peek();
                // Another kind of list, or (at the top level) another list altogether: start a new one.
                if (top.Tag != item.Tag || (target == 1 && top.NumId != item.NumId)) CloseList(lists);
            }

            while (lists.Count < target)
            {
                if (lists.Count > 0 && !lists.Peek().ItemOpen)
                {
                    _html.Append("<li>");
                    lists.Peek().ItemOpen = true;
                }
                _html.Append('<').Append(item.Tag);
                if (lists.Count + 1 == target)
                {
                    if (item.Type != null) _html.Append(" type=\"").Append(item.Type).Append('"');
                    if (item.Start != 1) _html.Append(" start=\"").Append(item.Start).Append('"');
                }
                _html.Append('>');
                lists.Push(new ListState { Tag = item.Tag, NumId = item.NumId });
            }

            var current = lists.Peek();
            if (current.ItemOpen) _html.Append("</li>");
            _html.Append("<li>").Append(inner);
            current.ItemOpen = true;
        }

        private void CloseList(Stack<ListState> lists)
        {
            var list = lists.Pop();
            if (list.ItemOpen) _html.Append("</li>");
            _html.Append("</").Append(list.Tag).Append('>');
        }

        private void CloseLists(Stack<ListState> lists)
        {
            while (lists.Count > 0) CloseList(lists);
        }

        // ------------------------------------------------------------ tables

        private sealed class Cell
        {
            public TableCell Source = null!;
            public int Column;
            public int ColSpan = 1;
            public int RowSpan = 1;
            public bool Continues;   // the lower part of a vertically merged cell: not written
            public bool Starts;
        }

        private void WriteTable(Table table, int depth)
        {
            if (depth >= MaxDepth) return;

            var rows = new List<(bool Header, List<Cell> Cells)>();
            foreach (var row in Rows(table))
            {
                var rowProperties = row.TableRowProperties;
                var column = rowProperties?.GetFirstChild<GridBefore>()?.Val?.Value ?? 0;
                var cells = new List<Cell>();
                foreach (var source in Cells(row))
                {
                    var properties = source.TableCellProperties;
                    var span = Math.Clamp(properties?.GridSpan?.Val?.Value ?? 1, 1, 100);
                    var merge = properties?.VerticalMerge;
                    var starts = merge?.Val != null && merge.Val.Value == MergedCellValues.Restart;
                    cells.Add(new Cell { Source = source, Column = column, ColSpan = span, Starts = starts, Continues = merge != null && !starts });
                    column += span;
                }
                if (cells.Count > 0) rows.Add((IsHeader(rowProperties?.GetFirstChild<TableHeader>()), cells));
            }
            if (rows.Count == 0) return;

            for (var r = 0; r < rows.Count; r++)
            {
                foreach (var cell in rows[r].Cells.Where(c => c.Starts))
                {
                    for (var below = r + 1; below < rows.Count; below++)
                    {
                        if (!rows[below].Cells.Any(c => c.Continues && c.Column == cell.Column)) break;
                        cell.RowSpan++;
                    }
                }
            }

            _html.Append("<div class=\"dfh-docx-table\"><table>");
            foreach (var (header, cells) in rows)
            {
                _html.Append("<tr>");
                foreach (var cell in cells.Where(c => !c.Continues))
                {
                    var tag = header ? "th" : "td";
                    _html.Append('<').Append(tag);
                    if (header) _html.Append(" scope=\"col\"");
                    if (cell.ColSpan > 1) _html.Append(" colspan=\"").Append(cell.ColSpan).Append('"');
                    if (cell.RowSpan > 1) _html.Append(" rowspan=\"").Append(cell.RowSpan).Append('"');
                    _html.Append('>');
                    Blocks(cell.Source, depth + 1);
                    _html.Append("</").Append(tag).Append('>');
                }
                _html.Append("</tr>");
            }
            _html.Append("</table></div>");
        }

        private static IEnumerable<TableRow> Rows(OpenXmlElement container)
        {
            foreach (var child in container.ChildElements)
            {
                if (child is TableRow row) yield return row;
                else if (child is SdtRow or SdtContentRow or CustomXmlRow)
                {
                    foreach (var inner in Rows(child)) yield return inner;
                }
            }
        }

        private static IEnumerable<TableCell> Cells(OpenXmlElement container)
        {
            foreach (var child in container.ChildElements)
            {
                if (child is TableCell cell) yield return cell;
                else if (child is SdtCell or SdtContentCell or CustomXmlCell)
                {
                    foreach (var inner in Cells(child)) yield return inner;
                }
            }
        }

        // ------------------------------------------------------------ inline

        private void Inline(OpenXmlElement parent, List<Segment> segments, string? paragraphStyleId, int depth)
        {
            if (depth > MaxDepth) return;
            foreach (var child in parent.ChildElements)
            {
                switch (child)
                {
                    case Run run:
                        WriteRun(run, segments, paragraphStyleId);
                        break;

                    case Hyperlink link:
                        var inner = new List<Segment>();
                        Inline(link, inner, paragraphStyleId, depth + 1);
                        var url = LinkUrl(link);
                        if (url == null)
                        {
                            foreach (var segment in inner) Add(segments, segment);
                        }
                        else
                        {
                            var anchor = new Segment { Atomic = true };
                            anchor.Html.Append("<a href=\"").Append(WebUtility.HtmlEncode(url)).Append('"');
                            if (!url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                            {
                                anchor.Html.Append(" rel=\"noopener noreferrer nofollow\" target=\"_blank\"");
                            }
                            anchor.Html.Append('>').Append(Join(inner)).Append("</a>");
                            segments.Add(anchor);
                        }
                        break;

                    // Tracked deletions and moved-away text are not part of the current document.
                    case DeletedRun or MoveFromRun:
                        break;

                    case InsertedRun or MoveToRun or SdtRun or SdtContentRun or SimpleField or CustomXmlRun:
                        Inline(child, segments, paragraphStyleId, depth + 1);
                        break;
                }
            }
        }

        private void WriteRun(Run run, List<Segment> segments, string? paragraphStyleId)
        {
            var properties = run.RunProperties;
            if (On(properties?.Vanish)) return;   // hidden text

            var runStyleId = properties?.RunStyle?.Val?.Value;
            var format = Fmt.None;
            if (Flag<Bold>(properties, runStyleId, paragraphStyleId)) format |= Fmt.Bold;
            if (Flag<Italic>(properties, runStyleId, paragraphStyleId)) format |= Fmt.Italic;
            if (Flag<Strike>(properties, runStyleId, paragraphStyleId) || On(properties?.DoubleStrike)) format |= Fmt.Strike;
            var underline = properties?.Underline?.Val;
            if (underline != null && underline.Value != UnderlineValues.None) format |= Fmt.Underline;
            var vertical = properties?.VerticalTextAlignment?.Val;
            if (vertical != null)
            {
                if (vertical.Value == VerticalPositionValues.Superscript) format |= Fmt.Super;
                else if (vertical.Value == VerticalPositionValues.Subscript) format |= Fmt.Sub;
            }

            var html = new StringBuilder();
            foreach (var child in run.ChildElements)
            {
                switch (child)
                {
                    case Text text:
                        html.Append(WebUtility.HtmlEncode(text.Text));
                        break;
                    case Break lineBreak:
                        if (lineBreak.Type != null && lineBreak.Type.Value == BreakValues.Page) _pageBreak = true;
                        else html.Append("<br>");
                        break;
                    case CarriageReturn:
                        html.Append("<br>");
                        break;
                    case TabChar:
                        html.Append(' ');
                        break;
                    case NoBreakHyphen:
                        html.Append('‑');
                        break;
                    case Drawing drawing:
                        html.Append(WritePicture(drawing));
                        break;
                    case DocumentFormat.OpenXml.Wordprocessing.Picture:
                        html.Append(Placeholder(null));   // legacy VML picture
                        break;
                }
            }
            if (html.Length == 0) return;

            var segment = new Segment { Format = format };
            segment.Html.Append(html);
            Add(segments, segment);
        }

        /// <summary>An on/off run property: the run's own setting, then its character style, then the paragraph style.</summary>
        private bool Flag<T>(RunProperties? properties, string? runStyleId, string? paragraphStyleId) where T : OnOffType
        {
            var own = properties?.GetFirstChild<T>();
            if (own != null) return On(own);
            foreach (var style in Chain(runStyleId).Concat(Chain(paragraphStyleId)))
            {
                var inherited = style.StyleRunProperties?.GetFirstChild<T>();
                if (inherited != null) return On(inherited);
            }
            return false;
        }

        private static void Add(List<Segment> segments, Segment segment)
        {
            var last = segments.Count > 0 ? segments[^1] : null;
            if (last != null && !last.Atomic && !segment.Atomic && last.Format == segment.Format)
            {
                last.Html.Append(segment.Html);
            }
            else
            {
                segments.Add(segment);
            }
        }

        private static string Join(List<Segment> segments)
        {
            var html = new StringBuilder();
            foreach (var segment in segments)
            {
                foreach (var (flag, tag) in FmtTags)
                {
                    if (segment.Format.HasFlag(flag)) html.Append('<').Append(tag).Append('>');
                }
                html.Append(segment.Html);
                for (var i = FmtTags.Length - 1; i >= 0; i--)
                {
                    if (segment.Format.HasFlag(FmtTags[i].Flag)) html.Append("</").Append(FmtTags[i].Tag).Append('>');
                }
            }
            return html.ToString();
        }

        /// <summary>The target of an external link when it is http, https or mailto; otherwise null (plain text).</summary>
        private string? LinkUrl(Hyperlink link)
        {
            var id = link.Id?.Value;
            if (string.IsNullOrEmpty(id)) return null;   // a bookmark inside the document
            try
            {
                var relationship = _main.HyperlinkRelationships.FirstOrDefault(r => r.Id == id);
                if (relationship == null || !relationship.IsExternal) return null;
                var url = PreviewRendering.Clean(relationship.Uri.OriginalString);
                if (url == null) return null;
                return PreviewRendering.IsWebUrl(url) || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ? url : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ---------------------------------------------------------- pictures

        private string WritePicture(Drawing drawing)
        {
            var properties = drawing.Descendants<DW.DocProperties>().FirstOrDefault();
            var alt = properties?.Description?.Value;
            if (string.IsNullOrWhiteSpace(alt)) alt = properties?.Title?.Value;
            alt = string.IsNullOrWhiteSpace(alt) ? null : alt.Trim();

            var id = drawing.Descendants<A.Blip>().FirstOrDefault()?.Embed?.Value;
            if (string.IsNullOrEmpty(id)) return Placeholder(alt);

            try
            {
                if (_main.GetPartById(id) is not ImagePart part || !PictureTypes.TryGetValue(part.ContentType, out var type))
                {
                    return Placeholder(alt);
                }

                byte[]? bytes;
                using (var stream = part.GetStream(FileMode.Open, FileAccess.Read))
                {
                    bytes = ReadUpTo(stream, _pictureBudget);
                }
                if (bytes == null || bytes.Length == 0) return Placeholder(alt);
                _pictureBudget -= bytes.Length;

                var data = Convert.ToBase64String(bytes);
                _pictureChars += data.Length;
                var html = new StringBuilder("<img class=\"dfh-docx-img\" src=\"data:").Append(type).Append(";base64,").Append(data).Append('"');
                html.Append(" alt=\"").Append(WebUtility.HtmlEncode(alt ?? string.Empty)).Append('"');
                var width = drawing.Descendants<DW.Extent>().FirstOrDefault()?.Cx?.Value;
                if (width is > 0)
                {
                    // 9525 EMU = 1 CSS pixel
                    html.Append(" width=\"").Append(Math.Clamp(width.Value / 9525, 1, 1600)).Append('"');
                }
                return html.Append('>').ToString();
            }
            catch (Exception)
            {
                return Placeholder(alt);
            }
        }

        /// <summary>The whole stream, or null when it holds more than <paramref name="limit"/> bytes.</summary>
        private static byte[]? ReadUpTo(Stream stream, int limit)
        {
            if (limit <= 0) return null;
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
            {
                if (buffer.Length + read > limit) return null;
                buffer.Write(chunk, 0, read);
            }
            return buffer.ToArray();
        }

        private static string Placeholder(string? alt) =>
            "<span class=\"dfh-docx-picture\">[" + (alt == null ? "picture" : "picture: " + WebUtility.HtmlEncode(alt)) + "]</span>";

        private static bool IsHeader(TableHeader? header) =>
            header != null && (header.Val == null || header.Val.Value != OnOffOnlyValues.Off);

        private static bool On(OnOffType? element) => element != null && (element.Val == null || element.Val.Value);
    }
}
