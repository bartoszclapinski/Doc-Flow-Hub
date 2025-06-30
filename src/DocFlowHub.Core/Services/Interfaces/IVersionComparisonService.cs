using DocFlowHub.Core.Models.AI;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Service for AI-powered version comparison and change analysis
/// </summary>
public interface IVersionComparisonService
{
    /// <summary>
    /// Compare two document versions and generate AI analysis
    /// </summary>
    Task<VersionComparison> CompareVersionsAsync(string oldContent, string newContent, string documentTitle);
    
    /// <summary>
    /// Get existing comparison between two document versions
    /// </summary>
    Task<VersionComparison?> GetComparisonAsync(int fromVersionId, int toVersionId);
} 