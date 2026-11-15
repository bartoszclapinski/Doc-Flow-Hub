using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Documents;

/// <summary>
/// Previews for the Details page (Sprint 18, decisions S18-1…S18-5). Access goes through
/// <see cref="IDocumentBrowserService.GetDetailsAsync"/>, the same check the Details page uses.
/// Read-only, AI-free, so it works in the demo.
/// </summary>
public class DocumentPreviewService : IDocumentPreviewService
{
    private static readonly Dictionary<string, string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
    };

    private static readonly Dictionary<string, string> ExtractedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    private readonly IDocumentBrowserService _browser;
    private readonly IDocumentStorageService _storage;
    private readonly ITextExtractionService _extraction;
    private readonly ILogger<DocumentPreviewService> _logger;

    public DocumentPreviewService(
        IDocumentBrowserService browser,
        IDocumentStorageService storage,
        ITextExtractionService extraction,
        ILogger<DocumentPreviewService> logger)
    {
        _browser = browser;
        _storage = storage;
        _extraction = extraction;
        _logger = logger;
    }

    public async Task<ServiceResult<DocumentPreview>> GetPreviewAsync(string userId, int documentId, int? versionNumber = null)
    {
        var details = await _browser.GetDetailsAsync(userId, documentId);
        if (!details.Succeeded || details.Data == null)
        {
            return ServiceResult<DocumentPreview>.Failure("Document not found");
        }
        return await GetPreviewAsync(details.Data, versionNumber);
    }

    public async Task<ServiceResult<DocumentPreview>> GetPreviewAsync(DocumentDetails document, int? versionNumber = null)
    {
        if (document.Versions.Count == 0)
        {
            return ServiceResult<DocumentPreview>.Success(new DocumentPreview
            {
                Kind = DocumentPreviewKind.None,
                FileType = Normalize(document.FileType),
                Note = "This document has no file yet.",
            });
        }

        var version = Pick(document, versionNumber);
        if (version == null)
        {
            return ServiceResult<DocumentPreview>.Failure("Version not found");
        }

        var type = Normalize(string.IsNullOrEmpty(version.FileType) ? document.FileType : version.FileType);
        var preview = new DocumentPreview
        {
            VersionNumber = version.Number,
            VersionId = version.Id,
            LatestVersionNumber = document.Versions[0].Number,
            FileType = type,
            FileSize = version.FileSize,
        };

        if (ImageTypes.ContainsKey(type))
        {
            preview.Kind = DocumentPreviewKind.Image;
            return ServiceResult<DocumentPreview>.Success(preview);
        }

        var isText = type is ".txt" or ".md";
        if (!isText && !ExtractedTypes.ContainsKey(type))
        {
            preview.Kind = DocumentPreviewKind.None;
            preview.Note = $"No preview for {Label(type)} files yet. Download the file to open it.";
            return ServiceResult<DocumentPreview>.Success(preview);
        }

        var file = await _storage.DownloadDocumentAsync(document.Id, version.Number);
        if (!file.Succeeded || file.Data == null)
        {
            _logger.LogWarning("Preview: could not read document {DocumentId} v{Version}: {Error}", document.Id, version.Number, file.Error);
            preview.Kind = DocumentPreviewKind.None;
            preview.Note = "The file could not be read for a preview. Try downloading it.";
            return ServiceResult<DocumentPreview>.Success(preview);
        }

        if (isText)
        {
            var (text, encoding, truncated) = PreviewRendering.DecodeText(file.Data, DocumentPreview.TextLimitBytes);
            preview.Text = text;
            preview.Encoding = encoding;
            preview.Truncated = truncated;
            if (type == ".md")
            {
                preview.Kind = DocumentPreviewKind.Markdown;
                preview.Html = PreviewRendering.RenderMarkdown(text);
            }
            else
            {
                preview.Kind = DocumentPreviewKind.Text;
            }
            return ServiceResult<DocumentPreview>.Success(preview);
        }

        // PDF / DOCX: the text the AI features use, until a page-layout preview exists (S18-1).
        string? extracted = null;
        try
        {
            var result = await _extraction.ExtractTextAsync(file.Data, "document" + type, ExtractedTypes[type]);
            if (result.Succeeded) extracted = result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Preview: text extraction failed for document {DocumentId} v{Version}", document.Id, version.Number);
        }

        if (string.IsNullOrWhiteSpace(extracted))
        {
            preview.Kind = DocumentPreviewKind.None;
            preview.Note = $"No text could be found in this {Label(type)} file (it may be scanned images). Download it to open it.";
            return ServiceResult<DocumentPreview>.Success(preview);
        }

        var (capped, cut) = PreviewRendering.CapText(extracted, DocumentPreview.TextLimitBytes);
        preview.Kind = DocumentPreviewKind.ExtractedText;
        preview.Text = capped;
        preview.Truncated = cut;
        return ServiceResult<DocumentPreview>.Success(preview);
    }

    public async Task<ServiceResult<DocumentPreviewFile>> GetInlineFileAsync(string userId, int documentId, int? versionNumber = null)
    {
        var details = await _browser.GetDetailsAsync(userId, documentId);
        if (!details.Succeeded || details.Data == null)
        {
            return ServiceResult<DocumentPreviewFile>.Failure("Document not found");
        }

        var version = Pick(details.Data, versionNumber);
        if (version == null)
        {
            return ServiceResult<DocumentPreviewFile>.Failure("Version not found");
        }

        var type = Normalize(string.IsNullOrEmpty(version.FileType) ? details.Data.FileType : version.FileType);
        if (!ImageTypes.TryGetValue(type, out var contentType))
        {
            return ServiceResult<DocumentPreviewFile>.Failure("No inline preview for this file type");
        }

        var file = await _storage.DownloadDocumentAsync(documentId, version.Number);
        if (!file.Succeeded || file.Data == null)
        {
            return ServiceResult<DocumentPreviewFile>.Failure(file.Error ?? "The file could not be read");
        }

        return ServiceResult<DocumentPreviewFile>.Success(new DocumentPreviewFile
        {
            Content = file.Data,
            ContentType = contentType,
            ETag = $"\"{documentId}-{version.Number}-{version.Hash ?? version.FileSize.ToString()}\"",
        });
    }

    private static DocumentDetailsVersion? Pick(DocumentDetails document, int? versionNumber) =>
        versionNumber == null
            ? document.Versions.FirstOrDefault()
            : document.Versions.FirstOrDefault(v => v.Number == versionNumber);

    private static string Normalize(string? fileType)
    {
        var ext = (fileType ?? string.Empty).Trim().ToLowerInvariant();
        return ext.Length == 0 || ext.StartsWith('.') ? ext : "." + ext;
    }

    private static string Label(string type) => type.TrimStart('.').ToUpperInvariant() is { Length: > 0 } label ? label : "these";
}
