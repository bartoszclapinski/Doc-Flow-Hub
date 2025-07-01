using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Common;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Service for AI-powered document summarization with caching
/// </summary>
public interface IDocumentSummaryService
{
    /// <summary>
    /// Get existing summary for a document from database with caching
    /// </summary>
    Task<ServiceResult<DocumentSummary>> GetSummaryAsync(int documentId);
    
    /// <summary>
    /// Generate a new AI summary for a document
    /// </summary>
    Task<ServiceResult<DocumentSummary>> GenerateSummaryAsync(int documentId);
    
    /// <summary>
    /// Generate a new AI summary for a document with specific model
    /// </summary>
    Task<ServiceResult<DocumentSummary>> GenerateSummaryAsync(int documentId, AIModel model);
    
    /// <summary>
    /// Regenerate summary for a document by deleting existing and creating new
    /// </summary>
    Task<ServiceResult> RegenerateSummaryAsync(int documentId);
    
    /// <summary>
    /// Regenerate summary for a document with specific model
    /// </summary>
    Task<ServiceResult> RegenerateSummaryAsync(int documentId, AIModel model);
    
    /// <summary>
    /// Delete existing summary for a document
    /// </summary>
    Task<ServiceResult> DeleteSummaryAsync(int documentId);
} 