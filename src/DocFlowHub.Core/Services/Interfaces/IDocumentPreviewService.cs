using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Browser;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Read-only previews for the Details page: text, Markdown, images, PDF pages (Sprint 19), formatted Word
/// documents (Sprint 20) and the plain text of PDF / DOCX files. Access is checked like the Details page; a failure means "not found or not yours".
/// </summary>
public interface IDocumentPreviewService
{
    /// <summary>
    /// The preview of one version (the newest when <paramref name="versionNumber"/> is null). The plain text of
    /// a PDF or a DOCX is only extracted when <paramref name="textView"/> is set; otherwise a PDF's pages
    /// render in the browser and a DOCX comes as formatted HTML.
    /// </summary>
    Task<ServiceResult<DocumentPreview>> GetPreviewAsync(string userId, int documentId, int? versionNumber = null, bool textView = false);

    /// <summary>
    /// The same for a document the caller already loaded with <c>IDocumentBrowserService.GetDetailsAsync</c>
    /// (which checked access), so the Details page does not load it twice.
    /// </summary>
    Task<ServiceResult<DocumentPreview>> GetPreviewAsync(DocumentDetails document, int? versionNumber = null, bool textView = false);

    /// <summary>
    /// The raw file of one version for inline display. Only images and PDFs are served; anything else
    /// fails, so the handler can answer 404.
    /// </summary>
    Task<ServiceResult<DocumentPreviewFile>> GetInlineFileAsync(string userId, int documentId, int? versionNumber = null);
}
