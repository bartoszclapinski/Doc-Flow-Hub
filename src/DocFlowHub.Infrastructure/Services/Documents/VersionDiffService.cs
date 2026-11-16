using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Documents;

/// <summary>
/// The text diff of two versions for the Compare page (Sprint 21, decisions S21-1…S21-7). Access goes
/// through <see cref="IDocumentBrowserService.GetComparisonAsync"/>, the same check the page uses.
/// Read-only, AI-free, computed on each request.
/// </summary>
public class VersionDiffService : IVersionDiffService
{
    private static readonly Dictionary<string, string> ExtractedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    private readonly IDocumentBrowserService _browser;
    private readonly IDocumentStorageService _storage;
    private readonly ITextExtractionService _extraction;
    private readonly ILogger<VersionDiffService> _logger;

    public VersionDiffService(
        IDocumentBrowserService browser,
        IDocumentStorageService storage,
        ITextExtractionService extraction,
        ILogger<VersionDiffService> logger)
    {
        _browser = browser;
        _storage = storage;
        _extraction = extraction;
        _logger = logger;
    }

    public async Task<ServiceResult<VersionDiff>> GetDiffAsync(string userId, int documentId, int fromVersion, int toVersion, VersionDiffOptions? options = null)
    {
        var comparison = await _browser.GetComparisonAsync(userId, documentId, fromVersion, toVersion);
        if (!comparison.Succeeded || comparison.Data == null)
        {
            return ServiceResult<VersionDiff>.Failure(comparison.Error ?? "Document not found");
        }
        return await GetDiffAsync(comparison.Data, options);
    }

    public async Task<ServiceResult<VersionDiff>> GetDiffAsync(DocumentComparisonView comparison, VersionDiffOptions? options = null)
    {
        var older = await ReadAsync(comparison, comparison.From);
        if (older.Problem != null) return ServiceResult<VersionDiff>.Success(older.Problem);
        var newer = await ReadAsync(comparison, comparison.To);
        if (newer.Problem != null) return ServiceResult<VersionDiff>.Success(newer.Problem);

        var diff = LineDiff.Compare(older.Lines, newer.Lines, options);
        if (diff.Status == VersionDiffStatus.Identical)
        {
            diff.Note = options?.IgnoreWhitespace == true
                ? $"v{comparison.From.Number} and v{comparison.To.Number} have the same text when whitespace is ignored."
                : $"v{comparison.From.Number} and v{comparison.To.Number} have the same text. The files may still differ in layout or formatting.";
        }
        return ServiceResult<VersionDiff>.Success(diff);
    }

    /// <summary>The lines of one version, or the reason there are none.</summary>
    private async Task<(string[] Lines, VersionDiff? Problem)> ReadAsync(DocumentComparisonView comparison, DocumentDetailsVersion version)
    {
        var type = Normalize(string.IsNullOrEmpty(version.FileType) ? comparison.FileType : version.FileType);
        var isText = type is ".txt" or ".md";
        if (!isText && !ExtractedTypes.ContainsKey(type))
        {
            return Problem(VersionDiffStatus.NoText,
                $"v{version.Number} is {Label(type)}, which has no text to compare. Download both versions to compare them.");
        }

        var file = await _storage.DownloadDocumentAsync(comparison.DocumentId, version.Number);
        if (!file.Succeeded || file.Data == null)
        {
            _logger.LogWarning("Diff: could not read document {DocumentId} v{Version}: {Error}", comparison.DocumentId, version.Number, file.Error);
            return Problem(VersionDiffStatus.Unreadable, $"The file of v{version.Number} could not be read. Try downloading it.");
        }

        string text;
        bool truncated;
        if (isText)
        {
            (text, _, truncated) = PreviewRendering.DecodeText(file.Data, VersionDiff.TextLimitBytes);
        }
        else
        {
            string? extracted = null;
            try
            {
                var result = await _extraction.ExtractTextAsync(file.Data, "document" + type, ExtractedTypes[type]);
                if (result.Succeeded) extracted = result.Data;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Diff: text extraction failed for document {DocumentId} v{Version}", comparison.DocumentId, version.Number);
            }
            if (string.IsNullOrWhiteSpace(extracted))
            {
                return Problem(VersionDiffStatus.NoText,
                    $"No text could be found in v{version.Number} (it may be scanned pages or a damaged file). Download both versions to compare them.");
            }
            (text, truncated) = PreviewRendering.CapText(extracted, VersionDiff.TextLimitBytes);
        }

        var lines = LineDiff.Lines(text);
        if (truncated || lines.Length > VersionDiff.LineLimit)
        {
            return Problem(VersionDiffStatus.TooLarge,
                $"v{version.Number} is too large to compare here (the limit is {VersionDiff.TextLimitBytes / 1024} KB of text and {VersionDiff.LineLimit:N0} lines). Download both versions to compare them.");
        }
        return (lines, null);
    }

    private static (string[] Lines, VersionDiff? Problem) Problem(VersionDiffStatus status, string note) =>
        (Array.Empty<string>(), new VersionDiff { Status = status, Note = note });

    private static string Normalize(string? fileType)
    {
        var ext = (fileType ?? string.Empty).Trim().ToLowerInvariant();
        return ext.Length == 0 || ext.StartsWith('.') ? ext : "." + ext;
    }

    private static string Label(string type) => type switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" => "an image",
        "" => "a file of an unknown type",
        _ => $"a {type.TrimStart('.').ToUpperInvariant()} file",
    };
}
