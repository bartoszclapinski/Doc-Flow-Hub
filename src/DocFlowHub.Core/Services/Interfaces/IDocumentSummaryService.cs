using DocFlowHub.Core.Models.AI;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Service for AI-powered document summarization
/// </summary>
public interface IDocumentSummaryService
{
    /// <summary>
    /// Generate a summary for document content
    /// </summary>
    Task<DocumentSummary> GenerateSummaryAsync(string documentContent, string documentTitle);
    
    /// <summary>
    /// Get existing summary for a document
    /// </summary>
    Task<DocumentSummary?> GetSummaryAsync(int documentId);
    
    /// <summary>
    /// Regenerate summary for a document
    /// </summary>
    Task<DocumentSummary> RegenerateSummaryAsync(int documentId);
} 