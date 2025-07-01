using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text;

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
    /// Extract text from PDF file
    /// </summary>
    private async Task<string> ExtractFromPdfFileAsync(byte[] fileContent, string fileName)
    {
        try
        {
            // TODO: Implement PDF text extraction using a library like iTextSharp or PdfPig
            // For now, return a placeholder indicating PDF processing is needed
            _logger.LogInformation("PDF text extraction not yet implemented for file: {FileName}", fileName);
            
            // Basic fallback - try to extract any readable text
            var fallbackText = await ExtractFallbackTextAsync(fileContent);
            return !string.IsNullOrWhiteSpace(fallbackText) 
                ? $"[PDF Content - Basic Extraction]\n{fallbackText}" 
                : "[PDF Document - Text extraction requires specialized library]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF file: {FileName}", fileName);
            return "[PDF Document - Text extraction failed]";
        }
    }

    /// <summary>
    /// Extract text from DOC file
    /// </summary>
    private async Task<string> ExtractFromDocFileAsync(byte[] fileContent, string fileName)
    {
        try
        {
            // TODO: Implement DOC text extraction using a library like Aspose.Words or similar
            _logger.LogInformation("DOC text extraction not yet implemented for file: {FileName}", fileName);
            
            var fallbackText = await ExtractFallbackTextAsync(fileContent);
            return !string.IsNullOrWhiteSpace(fallbackText) 
                ? $"[DOC Content - Basic Extraction]\n{fallbackText}" 
                : "[DOC Document - Text extraction requires specialized library]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DOC file: {FileName}", fileName);
            return "[DOC Document - Text extraction failed]";
        }
    }

    /// <summary>
    /// Extract text from DOCX file
    /// </summary>
    private async Task<string> ExtractFromDocxFileAsync(byte[] fileContent, string fileName)
    {
        try
        {
            // TODO: Implement DOCX text extraction using DocumentFormat.OpenXml
            _logger.LogInformation("DOCX text extraction not yet implemented for file: {FileName}", fileName);
            
            var fallbackText = await ExtractFallbackTextAsync(fileContent);
            return !string.IsNullOrWhiteSpace(fallbackText) 
                ? $"[DOCX Content - Basic Extraction]\n{fallbackText}" 
                : "[DOCX Document - Text extraction requires specialized library]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DOCX file: {FileName}", fileName);
            return "[DOCX Document - Text extraction failed]";
        }
    }

    /// <summary>
    /// Fallback text extraction for binary files - attempts to find readable text
    /// </summary>
    private async Task<string> ExtractFallbackTextAsync(byte[] fileContent)
    {
        try
        {
            var text = Encoding.UTF8.GetString(fileContent);
            var builder = new StringBuilder();
            
            // Extract sequences of printable characters
            var currentWord = new StringBuilder();
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c))
                {
                    currentWord.Append(c);
                }
                else
                {
                    if (currentWord.Length > 3) // Only keep words with 4+ characters
                    {
                        builder.Append(currentWord.ToString().Trim()).Append(' ');
                    }
                    currentWord.Clear();
                }
            }
            
            // Add the last word if it's long enough
            if (currentWord.Length > 3)
            {
                builder.Append(currentWord.ToString().Trim());
            }

            var extractedText = builder.ToString().Trim();
            
            // Clean up multiple spaces and limit length
            var cleanedText = System.Text.RegularExpressions.Regex.Replace(extractedText, @"\s+", " ");
            
            return await Task.FromResult(cleanedText.Length > 500 ? cleanedText.Substring(0, 500) + "..." : cleanedText);
        }
        catch
        {
            return await Task.FromResult(string.Empty);
        }
    }
} 