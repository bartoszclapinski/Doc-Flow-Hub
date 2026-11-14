using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Browser;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Read-only queries behind the Documents screen. A user sees the documents they own and the ones
/// shared with a team they belong to (the same rule as <see cref="IDocumentService"/>).
/// </summary>
public interface IDocumentBrowserService
{
    /// <summary>Projects and folders with document counts, the user's teams and the filter options.</summary>
    Task<ServiceResult<DocumentTree>> GetTreeAsync(string userId);

    /// <summary>One page of documents matching the query.</summary>
    Task<ServiceResult<DocumentListPage>> GetDocumentsAsync(string userId, DocumentListQuery query);

    /// <summary>Inspector data for one document, or a failure when the user cannot see it.</summary>
    Task<ServiceResult<DocumentInspector>> GetInspectorAsync(string userId, int documentId);

    /// <summary>The Details page: the document, all versions, AI summary with key points, saved comparisons.</summary>
    Task<ServiceResult<DocumentDetails>> GetDetailsAsync(string userId, int documentId);

    /// <summary>
    /// The Compare page for two version numbers (order does not matter; null = the two newest). Reads
    /// the saved AI comparison only, it never calls the AI.
    /// </summary>
    Task<ServiceResult<DocumentComparisonView>> GetComparisonAsync(string userId, int documentId, int? fromVersion, int? toVersion);
}
