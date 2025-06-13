using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocFlowHub.Web.Pages.Documents;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentCategoryService _categoryService;

    public IndexModel(
        IDocumentService documentService,
        IDocumentCategoryService categoryService)
    {
        _documentService = documentService;
        _categoryService = categoryService;
    }

    public PagedResult<DocumentDto> Documents { get; set; } = new();
    public IEnumerable<DocumentCategoryDto> Categories { get; set; } = new List<DocumentCategoryDto>();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public DocumentFilter Filter { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var categoriesResult = await _categoryService.GetAllCategoriesAsync();
        if (!categoriesResult.Succeeded)
        {
            ErrorMessage = categoriesResult.Error;
            return Page();
        }
        Categories = categoriesResult.Data;

        var documentsResult = await _documentService.GetDocumentsAsync(Filter);
        if (!documentsResult.Succeeded)
        {
            ErrorMessage = documentsResult.Error;
            return Page();
        }
        Documents = documentsResult.Data;

        return Page();
    }

    public async Task<IActionResult> OnPostDownloadAsync(int documentId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get document details first to check permissions and get current version
        var documentResult = await _documentService.GetDocumentByIdAsync(documentId);
        if (!documentResult.Succeeded)
        {
            ErrorMessage = documentResult.Error;
            return Page();
        }

        var document = documentResult.Data;

        // Verify user has access to this document
        if (document.OwnerId != userId && document.TeamId == null)
        {
            return Forbid();
        }

        // Download the current version
        if (document.CurrentVersionId == null)
        {
            ErrorMessage = "Document has no current version";
            return Page();
        }

        var downloadResult = await _documentService.DownloadDocumentVersionAsync(documentId, document.CurrentVersionId.Value);
        if (!downloadResult.Succeeded)
        {
            ErrorMessage = downloadResult.Error;
            return Page();
        }

        var contentType = GetContentType(document.FileType);
        var fileName = $"{document.Title}{document.FileType}";

        return File(downloadResult.Data, contentType, fileName);
    }

    private static string GetContentType(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".md" => "text/markdown",
            ".txt" => "text/plain",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
} 