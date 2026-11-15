using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Browser;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Read-only previews for the Details page (Sprint 18): text, Markdown, images, and text extracted from
/// PDF / DOCX. Access is checked like the Details page; a failure means "not found or not yours".
/// </summary>
public interface IDocumentPreviewService
{
    /// <summary>The preview of one version (the newest when <paramref name="versionNumber"/> is null).</summary>
    Task<ServiceResult<DocumentPreview>> GetPreviewAsync(string userId, int documentId, int? versionNumber = null);

    /// <summary>
    /// The same for a document the caller already loaded with <c>IDocumentBrowserService.GetDetailsAsync</c>
    /// (which checked access), so the Details page does not load it twice.
    /// </summary>
    Task<ServiceResult<DocumentPreview>> GetPreviewAsync(DocumentDetails document, int? versionNumber = null);

    /// <summary>
    /// The raw file of one version for inline display. Only image types are served; anything else fails,
    /// so the handler can answer 404.
    /// </summary>
    Task<ServiceResult<DocumentPreviewFile>> GetInlineFileAsync(string userId, int documentId, int? versionNumber = null);
}
