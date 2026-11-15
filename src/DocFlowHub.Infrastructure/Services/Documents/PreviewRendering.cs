using System.Text;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace DocFlowHub.Infrastructure.Services.Documents;

/// <summary>
/// Pure helpers behind the document preview (Sprint 18): decoding text files and rendering Markdown
/// to HTML that is safe to put in the page.
/// </summary>
public static class PreviewRendering
{
    static PreviewRendering()
    {
        // Windows-1250 (the fallback for old Polish text files) lives in the code-pages provider.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Decodes up to <paramref name="limitBytes"/> bytes of a text file. Encoding: a BOM (UTF-8, UTF-16 LE/BE)
    /// wins; otherwise strict UTF-8; otherwise Windows-1250. Line endings become "\n".
    /// </summary>
    public static (string Text, string Encoding, bool Truncated) DecodeText(byte[] bytes, int limitBytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            var (slice, cut) = Slice(bytes, 3, limitBytes, Utf8Boundary);
            return (Normalize(new UTF8Encoding(false).GetString(slice)), "UTF-8", cut);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            var (slice, cut) = Slice(bytes, 2, limitBytes, EvenBoundary);
            return (Normalize(Encoding.Unicode.GetString(slice)), "UTF-16", cut);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            var (slice, cut) = Slice(bytes, 2, limitBytes, EvenBoundary);
            return (Normalize(Encoding.BigEndianUnicode.GetString(slice)), "UTF-16", cut);
        }

        var (body, truncated) = Slice(bytes, 0, limitBytes, Utf8Boundary);
        try
        {
            var strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return (Normalize(strict.GetString(body)), "UTF-8", truncated);
        }
        catch (DecoderFallbackException)
        {
            (body, truncated) = Slice(bytes, 0, limitBytes, (_, end) => end);
            return (Normalize(Encoding.GetEncoding(1250).GetString(body)), "Windows-1250", truncated);
        }
    }

    /// <summary>Cuts already-decoded text (e.g. extracted from a PDF) to roughly the same limit.</summary>
    public static (string Text, bool Truncated) CapText(string text, int limitChars)
    {
        text = Normalize(text);
        return text.Length <= limitChars ? (text, false) : (text[..limitChars], true);
    }

    private static (byte[] Slice, bool Truncated) Slice(byte[] bytes, int start, int limit, Func<byte[], int, int> boundary)
    {
        var available = bytes.Length - start;
        if (available <= limit) return (bytes[start..], false);
        var end = boundary(bytes, start + limit);
        return (bytes[start..end], true);
    }

    /// <summary>Moves a cut back so it does not split a UTF-8 sequence (continuation bytes are 10xxxxxx).</summary>
    private static int Utf8Boundary(byte[] bytes, int end)
    {
        var floor = Math.Max(0, end - 4);
        while (end > floor && (bytes[end] & 0xC0) == 0x80) end--;
        return end;
    }

    private static int EvenBoundary(byte[] bytes, int end) => end % 2 == 0 ? end : end - 1;

    private static string Normalize(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

    // ---------------------------------------------------------------- Markdown

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()            // raw HTML in the file is shown as text, never as markup
        .UsePipeTables()
        .UseTaskLists()
        .UseAutoLinks()
        .UseEmphasisExtras()
        .Build();

    /// <summary>
    /// Markdown → HTML for the preview. Raw HTML is disabled; links keep only http, https, mailto and
    /// #anchors (anything else becomes plain text); external links open in a new tab without a referrer;
    /// images are never loaded: an http(s) image becomes a link "image: alt", any other image its alt text.
    /// </summary>
    public static string RenderMarkdown(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);

        // Innermost first, so replacing an outer link never touches a detached inner one.
        var links = document.Descendants<LinkInline>().ToList();
        links.Reverse();
        foreach (var link in links)
        {
            var url = Clean(link.GetDynamicUrl?.Invoke() ?? link.Url);
            if (link.IsImage)
            {
                var alt = PlainText(link);
                var label = "image: " + (string.IsNullOrWhiteSpace(alt) ? "untitled" : alt);
                if (IsWebUrl(url))
                {
                    var replacement = new LinkInline(url!, link.Title ?? string.Empty);
                    replacement.AppendChild(new LiteralInline(label));
                    MarkExternal(replacement);
                    link.ReplaceBy(replacement);
                }
                else
                {
                    link.ReplaceBy(new LiteralInline("[" + label + "]"));
                }
                continue;
            }

            if (url == null || !IsAllowedLink(url))
            {
                link.ReplaceBy(new LiteralInline(PlainText(link)));
                continue;
            }

            link.Url = url;
            if (IsWebUrl(url)) MarkExternal(link);
        }

        foreach (var autolink in document.Descendants<AutolinkInline>().ToList())
        {
            if (!IsAllowedLink(Clean(autolink.Url) ?? string.Empty))
            {
                autolink.ReplaceBy(new LiteralInline(autolink.Url));
            }
            else if (!autolink.IsEmail)
            {
                MarkExternal(autolink);
            }
        }

        return document.ToHtml(Pipeline);
    }

    private static void MarkExternal(Inline inline)
    {
        var attributes = inline.GetAttributes();
        attributes.AddPropertyIfNotExist("rel", "noopener noreferrer nofollow");
        attributes.AddPropertyIfNotExist("target", "_blank");
    }

    /// <summary>Browsers ignore whitespace and control characters inside a URL ("java\tscript:"), so drop them first.</summary>
    private static string? Clean(string? url) =>
        url == null ? null : new string(url.Where(c => !char.IsWhiteSpace(c) && !char.IsControl(c)).ToArray());

    private static bool IsWebUrl(string? url) =>
        url != null && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    private static bool IsAllowedLink(string url) =>
        IsWebUrl(url) || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || url.StartsWith('#');

    private static string PlainText(ContainerInline container)
    {
        var builder = new StringBuilder();
        foreach (var inline in container.Descendants<Inline>())
        {
            switch (inline)
            {
                case LiteralInline literal: builder.Append(literal.Content.ToString()); break;
                case CodeInline code: builder.Append(code.Content); break;
            }
        }
        return builder.ToString();
    }
}
