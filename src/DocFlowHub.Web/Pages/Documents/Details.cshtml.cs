using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;

namespace DocFlowHub.Web.Pages.Documents
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentCategoryService _categoryService;

        public DetailsModel(
            IDocumentService documentService,
            IDocumentCategoryService categoryService)
        {
            _documentService = documentService;
            _categoryService = categoryService;
        }

        public DocumentDto Document { get; set; } = null!;
        public PagedResult<DocumentVersionDto> Versions { get; set; } = new();
        public IEnumerable<DocumentCategoryDto> Categories { get; set; } = new List<DocumentCategoryDto>();
        public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                return NotFound();
            }

            Document = documentResult.Data;

            // Verify user has access to this document
            if (Document.OwnerId != userId && Document.TeamId == null)
            {
                return Forbid();
            }

            // Load version history
            var versionsResult = await _documentService.GetDocumentVersionsAsync(Id, new DocumentFilter 
            { 
                PageNumber = PageNumber, 
                PageSize = 10 
            });
            
            if (versionsResult.Succeeded)
            {
                Versions = versionsResult.Data;
            }

            // Load categories
            var categoriesResult = await _categoryService.GetDocumentCategoriesAsync(Id);
            if (categoriesResult.Succeeded)
            {
                Categories = categoriesResult.Data;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDownloadAsync(int versionId)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Get document details first to check permissions
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
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

            // Get version details
            var versionResult = await _documentService.GetDocumentVersionAsync(Id, versionId);
            if (!versionResult.Succeeded)
            {
                ErrorMessage = "Version not found";
                return Page();
            }

            var version = versionResult.Data;

            // Download the file
            var downloadResult = await _documentService.DownloadDocumentVersionAsync(Id, versionId);
            if (!downloadResult.Succeeded)
            {
                ErrorMessage = downloadResult.Error;
                return Page();
            }

            var contentType = GetContentType(version.FileType);
            var fileName = $"{document.Title}_v{version.VersionNumber}{version.FileType}";

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
} 