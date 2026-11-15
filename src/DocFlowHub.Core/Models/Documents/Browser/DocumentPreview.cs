namespace DocFlowHub.Core.Models.Documents.Browser;

/// <summary>How a document version is shown in the Details preview (Sprint 18).</summary>
public enum DocumentPreviewKind
{
    /// <summary>No preview for this type (e.g. .doc): the page offers a download.</summary>
    None,

    /// <summary>Plain text (.txt), shown with line numbers.</summary>
    Text,

    /// <summary>Markdown (.md): sanitised HTML in <see cref="DocumentPreview.Html"/>, source in <see cref="DocumentPreview.Text"/>.</summary>
    Markdown,

    /// <summary>Text extracted from a DOCX until a page-layout preview exists.</summary>
    ExtractedText,

    /// <summary>JPG / PNG / GIF, loaded from the inline file handler.</summary>
    Image,

    /// <summary>
    /// PDF pages rendered in the browser by PDF.js from the inline file handler (Sprint 19).
    /// <see cref="DocumentPreview.Text"/> holds the extracted text only when the text view asked for it.
    /// </summary>
    Pdf,
}

/// <summary>The preview of one document version. Built by <c>IDocumentPreviewService</c>.</summary>
public class DocumentPreview
{
    public DocumentPreviewKind Kind { get; set; }

    /// <summary>The version shown (number, not id) and the newest version number.</summary>
    public int VersionNumber { get; set; }
    public int VersionId { get; set; }
    public int LatestVersionNumber { get; set; }
    public bool IsLatest => VersionNumber == LatestVersionNumber;

    /// <summary>".pdf", ".md", ... of the version shown.</summary>
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    /// <summary>Decoded text (Text, Markdown source, ExtractedText); capped at <see cref="TextLimitBytes"/>.</summary>
    public string? Text { get; set; }

    /// <summary>Rendered, sanitised Markdown (Markdown only).</summary>
    public string? Html { get; set; }

    /// <summary>True when the file had more text than the preview shows.</summary>
    public bool Truncated { get; set; }

    /// <summary>Encoding the text was read with ("UTF-8", "UTF-16", "Windows-1250"); null for non-text kinds.</summary>
    public string? Encoding { get; set; }

    /// <summary>Why there is no preview (unknown type, the file could not be read, nothing to show).</summary>
    public string? Note { get; set; }

    public const int TextLimitBytes = 256 * 1024;
}

/// <summary>An image or a PDF served inline by the Details file handler.</summary>
public class DocumentPreviewFile
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string? ETag { get; set; }
}
