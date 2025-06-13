using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;

namespace DocFlowHub.Web.Pages.Documents
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentCategoryService _categoryService;
        private readonly ITeamService _teamService;

        public DetailsModel(
            IDocumentService documentService,
            IDocumentCategoryService categoryService,
            ITeamService teamService)
        {
            _documentService = documentService;
            _categoryService = categoryService;
            _teamService = teamService;
        }

        public DocumentDto Document { get; set; } = null!;
        public PagedResult<DocumentVersionDto> Versions { get; set; } = new();
        public IEnumerable<DocumentCategoryDto> Categories { get; set; } = new List<DocumentCategoryDto>();
        public IEnumerable<TeamDto> UserTeams { get; set; } = new List<TeamDto>();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty]
        public int? ShareWithTeamId { get; set; }

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

            // Verify user has access to this document using secure method
            var accessResult = await _documentService.CanUserAccessDocumentAsync(Id, userId);
            if (!accessResult.Succeeded || !accessResult.Data)
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

            // Load user teams for sharing
            var teamsResult = await _teamService.GetUserTeamsAsync(userId, new TeamFilter());
            if (teamsResult.Succeeded)
            {
                UserTeams = teamsResult.Data.Items;
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

            // Verify user has access to this document using secure method
            var accessResult = await _documentService.CanUserAccessDocumentAsync(Id, userId);
            if (!accessResult.Succeeded || !accessResult.Data)
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

            // Get document details for filename
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                return Page();
            }

            var document = documentResult.Data;

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

        public async Task<IActionResult> OnPostShareWithTeamAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify user owns this document (only owners can share)
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                return Page();
            }

            var document = documentResult.Data;
            if (document.OwnerId != userId)
            {
                ErrorMessage = "Only document owners can share documents with teams";
                return Page();
            }

            if (!ShareWithTeamId.HasValue)
            {
                ErrorMessage = "Please select a team to share with";
                return Page();
            }

            var shareResult = await _documentService.ShareDocumentWithTeamAsync(Id, ShareWithTeamId.Value);
            if (shareResult.Succeeded)
            {
                SuccessMessage = "Document successfully shared with the team!";
            }
            else
            {
                ErrorMessage = shareResult.Error;
            }

            return RedirectToPage(new { id = Id });
        }

        public async Task<IActionResult> OnPostUnshareFromTeamAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify user owns this document (only owners can unshare)
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                return Page();
            }

            var document = documentResult.Data;
            if (document.OwnerId != userId)
            {
                ErrorMessage = "Only document owners can unshare documents from teams";
                return Page();
            }

            var unshareResult = await _documentService.UnshareDocumentFromTeamAsync(Id);
            if (unshareResult.Succeeded)
            {
                SuccessMessage = "Document successfully unshared from the team!";
            }
            else
            {
                ErrorMessage = unshareResult.Error;
            }

            return RedirectToPage(new { id = Id });
        }
    }
} 