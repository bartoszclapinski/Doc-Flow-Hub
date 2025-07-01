using DocFlowHub.Core.Models.Common;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Service for extracting text content from various document file types
/// </summary>
public interface ITextExtractionService
{
    /// <summary>
    /// Extract text content from a document file
    /// </summary>
    /// <param name="fileContent">The raw file content as byte array</param>
    /// <param name="fileName">The file name with extension</param>
    /// <param name="mimeType">The MIME type of the file</param>
    /// <returns>ServiceResult with extracted text content</returns>
    Task<ServiceResult<string>> ExtractTextAsync(byte[] fileContent, string fileName, string mimeType);

    /// <summary>
    /// Extract text content from a document by downloading it from storage
    /// </summary>
    /// <param name="documentId">The document ID</param>
    /// <param name="versionNumber">The version number to extract text from</param>
    /// <returns>ServiceResult with extracted text content</returns>
    Task<ServiceResult<string>> ExtractTextFromDocumentAsync(int documentId, int versionNumber);

    /// <summary>
    /// Check if the given file type is supported for text extraction
    /// </summary>
    /// <param name="fileName">The file name with extension</param>
    /// <param name="mimeType">The MIME type of the file</param>
    /// <returns>True if the file type is supported</returns>
    bool IsFileTypeSupported(string fileName, string? mimeType = null);

    /// <summary>
    /// Get supported file extensions for text extraction
    /// </summary>
    /// <returns>List of supported file extensions</returns>
    IEnumerable<string> GetSupportedExtensions();
} 