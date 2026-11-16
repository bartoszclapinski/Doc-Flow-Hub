using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Browser;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Line-by-line text diff of two versions for the Compare page (Sprint 21). Read-only and AI-free.
/// TXT and MD are compared as their text; PDF and DOCX as their extracted text.
/// </summary>
public interface IVersionDiffService
{
    /// <summary>
    /// The diff of two version numbers (order does not matter). Access is checked like the Compare page;
    /// a failure means "not found or not yours".
    /// </summary>
    Task<ServiceResult<VersionDiff>> GetDiffAsync(string userId, int documentId, int fromVersion, int toVersion, VersionDiffOptions? options = null);

    /// <summary>
    /// The same for a comparison the caller already loaded with
    /// <c>IDocumentBrowserService.GetComparisonAsync</c> (which checked access).
    /// </summary>
    Task<ServiceResult<VersionDiff>> GetDiffAsync(DocumentComparisonView comparison, VersionDiffOptions? options = null);
}
