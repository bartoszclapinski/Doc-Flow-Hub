using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocFlowHub.Infrastructure.Services.AI;

/// <summary>
/// Service for extracting text content from various document file types
/// </summary>
public class TextExtractionService : ITextExtractionService
{
    private readonly IDocumentStorageService _storageService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TextExtractionService> _logger;

    // Supported file extensions for text extraction
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".pdf", ".doc", ".docx"
    };

    // MIME type mappings
    private static readonly Dictionary<string, string> MimeTypeMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".txt", "text/plain" },
        { ".md", "text/markdown" },
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
    };

    public TextExtractionService(
        IDocumentStorageService storageService,
        ApplicationDbContext context,
        ILogger<TextExtractionService> logger)
    {
        _storageService = storageService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Extract text content from a document file
    /// </summary>
    public async Task<ServiceResult<string>> ExtractTextAsync(byte[] fileContent, string fileName, string mimeType)
    {
        try
        {
            _logger.LogInformation("Extracting text from file: {FileName} (MIME: {MimeType})", fileName, mimeType);

            if (fileContent == null || fileContent.Length == 0)
            {
                return ServiceResult<string>.Failure("File content is empty");
            }

            var extension = Path.GetExtension(fileName);
            if (!IsFileTypeSupported(fileName, mimeType))
            {
                return ServiceResult<string>.Failure($"File type '{extension}' is not supported for text extraction");
            }

            var extractedText = await ExtractTextByExtensionAsync(fileContent, extension, fileName);
            
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return ServiceResult<string>.Failure("No text content could be extracted from the file");
            }

            _logger.LogInformation("Successfully extracted {Length} characters from file: {FileName}", 
                extractedText.Length, fileName);

            return ServiceResult<string>.Success(extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from file: {FileName}", fileName);
            return ServiceResult<string>.Failure($"Failed to extract text: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract text content from a document by downloading it from storage
    /// </summary>
    public async Task<ServiceResult<string>> ExtractTextFromDocumentAsync(int documentId, int versionNumber)
    {
        try
        {
            _logger.LogInformation("Extracting text from document ID: {DocumentId}, Version: {VersionNumber}", 
                documentId, versionNumber);

            // Get document version details from database
            var documentVersion = await _context.DocumentVersions
                .Include(v => v.Document)
                .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.VersionNumber == versionNumber);

            if (documentVersion == null)
            {
                return ServiceResult<string>.Failure($"Document version not found: DocumentId={documentId}, Version={versionNumber}");
            }

            // Download document from storage
            var downloadResult = await _storageService.DownloadDocumentAsync(documentId, versionNumber);
            if (!downloadResult.Succeeded)
            {
                return ServiceResult<string>.Failure($"Failed to download document: {downloadResult.Error}");
            }

            // Use the file information from the database
            var fileExtension = documentVersion.FileType.StartsWith('.') ? documentVersion.FileType : $".{documentVersion.FileType}";
            var fileName = $"{documentVersion.Document.Title}{fileExtension}";
            var mimeType = MimeTypeMappings.TryGetValue(fileExtension, out var mappedMimeType) 
                ? mappedMimeType 
                : "application/octet-stream";

            // Extract text from downloaded content
            return await ExtractTextAsync(downloadResult.Data!, fileName, mimeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from document ID: {DocumentId}, Version: {VersionNumber}", 
                documentId, versionNumber);
            return ServiceResult<string>.Failure($"Failed to extract text from document: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if the given file type is supported for text extraction
    /// </summary>
    public bool IsFileTypeSupported(string fileName, string? mimeType = null)
    {
        var extension = Path.GetExtension(fileName);
        return SupportedExtensions.Contains(extension);
    }

    /// <summary>
    /// Get supported file extensions for text extraction
    /// </summary>
    public IEnumerable<string> GetSupportedExtensions()
    {
        return SupportedExtensions.ToList().AsReadOnly();
    }

    /// <summary>
    /// Extract text based on file extension
    /// </summary>
    private async Task<string> ExtractTextByExtensionAsync(byte[] fileContent, string extension, string fileName)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" => await ExtractFromTextFileAsync(fileContent),
            ".md" => await ExtractFromMarkdownFileAsync(fileContent),
            ".pdf" => await ExtractFromPdfFileAsync(fileContent, fileName),
            ".doc" => await ExtractFromDocFileAsync(fileContent, fileName),
            ".docx" => await ExtractFromDocxFileAsync(fileContent, fileName),
            _ => throw new NotSupportedException($"File extension '{extension}' is not supported")
        };
    }

    /// <summary>
    /// Extract text from plain text file
    /// </summary>
    private async Task<string> ExtractFromTextFileAsync(byte[] fileContent)
    {
        try
        {
            // Try UTF-8 first
            var text = Encoding.UTF8.GetString(fileContent);
            
            // Check for BOM and invalid characters that might indicate wrong encoding
            if (text.Contains('\uFFFD')) // Unicode replacement character indicates encoding issues
            {
                // Try other common encodings
                foreach (var encoding in new[] { Encoding.ASCII, Encoding.UTF32, Encoding.Unicode })
                {
                    try
                    {
                        var alternativeText = encoding.GetString(fileContent);
                        if (!alternativeText.Contains('\uFFFD'))
                        {
                            text = alternativeText;
                            break;
                        }
                    }
                    catch
                    {
                        // Continue with next encoding
                    }
                }
            }

            return await Task.FromResult(text.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract text using standard encodings, returning raw UTF-8");
            return Encoding.UTF8.GetString(fileContent).Trim();
        }
    }

    /// <summary>
    /// Extract text from Markdown file (similar to text file)
    /// </summary>
    private async Task<string> ExtractFromMarkdownFileAsync(byte[] fileContent)
    {
        // For now, treat markdown as plain text
        // TODO: Consider using a markdown parser to extract clean text without formatting
        return await ExtractFromTextFileAsync(fileContent);
    }

    /// <summary>
    /// Extract text from a PDF file using PdfPig (content-ordered, page by page).
    /// Returns an empty string on failure so the caller reports "no text extracted"
    /// rather than feeding a placeholder to the AI summarizer.
    /// </summary>
    private Task<string> ExtractFromPdfFileAsync(byte[] fileContent, string fileName)
    {
        try
        {
            using var pdf = PdfDocument.Open(fileContent);
            var builder = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                var pageText = ContentOrderTextExtractor.GetText(page);
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    builder.AppendLine(pageText);
                }
            }
            return Task.FromResult(builder.ToString().Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF file: {FileName}", fileName);
            return Task.FromResult(string.Empty);
        }
    }

    /// <summary>
    /// Legacy binary .doc (OLE2/Word 97-2003). No maintained free/OSS .NET extractor is
    /// available (NPOI ships no HWPF; the alternatives are commercial/limited), so this
    /// degrades gracefully: it logs and returns an empty string, which makes the caller
    /// report "no text could be extracted" instead of feeding garbage to the summarizer.
    /// Modern .docx is fully supported via <see cref="ExtractFromDocxFileAsync"/>.
    /// </summary>
    private Task<string> ExtractFromDocFileAsync(byte[] fileContent, string fileName)
    {
        _logger.LogWarning(
            "Legacy binary .doc text extraction is not supported (no OSS extractor). " +
            "Skipping AI text extraction for file: {FileName}. Re-save as .docx or .pdf for AI summaries.",
            fileName);
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// Extract text from a .docx file using the OpenXML SDK, one paragraph per line.
    /// Returns an empty string on failure.
    /// </summary>
    private Task<string> ExtractFromDocxFileAsync(byte[] fileContent, string fileName)
    {
        try
        {
            using var stream = new MemoryStream(fileContent);
            using var wordDoc = WordprocessingDocument.Open(stream, false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body == null)
            {
                return Task.FromResult(string.Empty);
            }

            var builder = new StringBuilder();
            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                var paragraphText = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    builder.AppendLine(paragraphText);
                }
            }
            return Task.FromResult(builder.ToString().Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from DOCX file: {FileName}", fileName);
            return Task.FromResult(string.Empty);
        }
    }
} 