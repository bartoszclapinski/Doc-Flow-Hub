using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Common;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Service for AI-powered version comparison and change analysis with caching
/// </summary>
public interface IVersionComparisonService
{
    /// <summary>
    /// Compare two document versions by ID and generate AI analysis
    /// </summary>
    Task<ServiceResult<VersionComparison>> CompareVersionsAsync(int fromVersionId, int toVersionId);
    
    /// <summary>
    /// Compare two document versions by ID and generate AI analysis with specific model
    /// </summary>
    Task<ServiceResult<VersionComparison>> CompareVersionsAsync(int fromVersionId, int toVersionId, AIModel model);
    
    /// <summary>
    /// Compare two document versions by ID and generate AI analysis with usage tracking
    /// </summary>
    Task<ServiceResult<VersionComparison>> CompareVersionsAsync(int fromVersionId, int toVersionId, string userId);
    
    /// <summary>
    /// Compare two document versions by ID and generate AI analysis with specific model and usage tracking
    /// </summary>
    Task<ServiceResult<VersionComparison>> CompareVersionsAsync(int fromVersionId, int toVersionId, AIModel model, string userId);
    
    /// <summary>
    /// Compare two document versions by version numbers and generate AI analysis
    /// </summary>
    Task<ServiceResult<VersionComparison>> CompareDocumentVersionsAsync(int documentId, int fromVersionNumber, int toVersionNumber);
    
    /// <summary>
    /// Compare two document versions by version numbers and generate AI analysis with specific model
    /// </summary>
    Task<ServiceResult<VersionComparison>> CompareDocumentVersionsAsync(int documentId, int fromVersionNumber, int toVersionNumber, AIModel model);
    
    /// <summary>
    /// Compare two document versions by version numbers and generate AI analysis with usage tracking
    /// </summary>
    Task<ServiceResult<VersionComparison>> CompareDocumentVersionsAsync(int documentId, int fromVersionNumber, int toVersionNumber, string userId);
    
    /// <summary>
    /// Compare two document versions by version numbers and generate AI analysis with specific model and usage tracking
    /// </summary>
    Task<ServiceResult<VersionComparison>> CompareDocumentVersionsAsync(int documentId, int fromVersionNumber, int toVersionNumber, AIModel model, string userId);
    
    /// <summary>
    /// Get existing comparison between two document versions
    /// </summary>
    Task<VersionComparison?> GetComparisonAsync(int fromVersionId, int toVersionId);
    
    /// <summary>
    /// Get all comparisons for a document
    /// </summary>
    Task<ServiceResult<IEnumerable<VersionComparison>>> GetDocumentComparisonsAsync(int documentId);
    
    /// <summary>
    /// Delete a version comparison
    /// </summary>
    Task<ServiceResult> DeleteComparisonAsync(int comparisonId);
} 