using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using DocFlowHub.Core.Models.Projects.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DocFlowHub.Web.Pages.Documents;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentCategoryService _categoryService;
    private readonly ITeamService _teamService;
    private readonly IProjectService _projectService;
    private readonly IFolderService _folderService;

    public IndexModel(
        IDocumentService documentService,
        IDocumentCategoryService categoryService,
        ITeamService teamService,
        IProjectService projectService,
        IFolderService folderService)
    {
        _documentService = documentService;
        _categoryService = categoryService;
        _teamService = teamService;
        _projectService = projectService;
        _folderService = folderService;
    }

    public PagedResult<DocumentDto> Documents { get; set; } = new();
    public IEnumerable<DocumentCategoryDto> Categories { get; set; } = new List<DocumentCategoryDto>();
    public IEnumerable<TeamDto> UserTeams { get; set; } = new List<TeamDto>();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public DocumentFilter Filter { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortDirection { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FolderId { get; set; }

    public IEnumerable<ProjectDto> Projects { get; set; } = new List<ProjectDto>();
    public IEnumerable<FolderDto> Folders { get; set; } = new List<FolderDto>();

    public SelectList ProjectSelectList => new SelectList(Projects, "Id", "Name", ProjectId);
    public SelectList FolderSelectList => new SelectList(Folders, "Id", "Name", FolderId);

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Handle sorting
        if (!string.IsNullOrEmpty(SortBy))
        {
            Filter.SortBy = SortBy;
            Filter.SortDirection = SortDirection ?? "asc";
        }

        // Set ViewData for sorting indicators
        SetSortViewData();

        var categoriesResult = await _categoryService.GetAllCategoriesAsync();
        if (!categoriesResult.Succeeded)
        {
            ErrorMessage = categoriesResult.Error;
            return Page();
        }
        Categories = categoriesResult.Data;

        // Load user's teams for team filter
        var teamsResult = await _teamService.GetUserTeamsAsync(userId, new TeamFilter());
        if (teamsResult.Succeeded)
        {
            UserTeams = teamsResult.Data.Items;
        }

        // Load projects & folders
        var projectsResult = await _projectService.GetActiveProjectsForUserAsync(userId);
        Projects = projectsResult.Succeeded ? projectsResult.Data : new List<ProjectDto>();
        if (ProjectId.HasValue && ProjectId.Value > 0)
        {
            await LoadFoldersAsync(userId);
        }

        // Build folder list even when no specific project selected (empty list)

        // apply project/folder filter
        if (ProjectId.HasValue)
            Filter.ProjectId = ProjectId;
        if (FolderId.HasValue)
            Filter.FolderId = FolderId;

        // Use secure method that only returns documents the user has access to
        var documentsResult = await _documentService.GetDocumentsForUserAsync(userId, Filter);
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

        // Verify user has access to this document using secure method
        var accessResult = await _documentService.CanUserAccessDocumentAsync(documentId, userId);
        if (!accessResult.Succeeded || !accessResult.Data)
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

    public async Task<IActionResult> OnPostDeleteAsync(int documentId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get document details first to check ownership
        var documentResult = await _documentService.GetDocumentByIdAsync(documentId);
        if (!documentResult.Succeeded)
        {
            TempData["ErrorMessage"] = documentResult.Error;
            return RedirectToPage();
        }

        var document = documentResult.Data;

        // Verify user owns this document (only owners can delete)
        if (document.OwnerId != userId)
        {
            TempData["ErrorMessage"] = "You can only delete documents that you own.";
            return RedirectToPage();
        }

        // Delete the document
        var deleteResult = await _documentService.DeleteDocumentAsync(documentId);
        if (!deleteResult.Succeeded)
        {
            TempData["ErrorMessage"] = deleteResult.Error;
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"Document '{document.Title}' has been successfully deleted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBulkDeleteAsync([FromBody] BulkDeleteRequest request)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return new JsonResult(new { success = false, error = "User not authenticated" });
        }

        if (request?.DocumentIds == null || !request.DocumentIds.Any())
        {
            return new JsonResult(new { success = false, error = "No documents selected for deletion" });
        }

        if (request.DocumentIds.Count > 100)
        {
            return new JsonResult(new { success = false, error = "Cannot delete more than 100 documents at once" });
        }

        try
        {
            // Call bulk delete service method
            var result = await _documentService.BulkDeleteDocumentsAsync(request.DocumentIds, userId);
            
            if (!result.Succeeded)
            {
                return new JsonResult(new { success = false, error = result.Error });
            }

            var bulkResult = result.Data;
            
            // Return detailed results
            return new JsonResult(new 
            { 
                success = true,
                totalRequested = bulkResult.TotalRequested,
                successfulDeletions = bulkResult.SuccessfulDeletions,
                failedDeletions = bulkResult.FailedDeletions,
                isFullySuccessful = bulkResult.IsFullySuccessful,
                hasPartialFailure = bulkResult.HasPartialFailure,
                results = bulkResult.Results.Select(r => new
                {
                    documentId = r.DocumentId,
                    documentTitle = r.DocumentTitle,
                    success = r.Success,
                    errorMessage = r.ErrorMessage
                })
            });
        }
        catch (Exception ex)
        {
            // Log the exception (assuming you have logging configured)
            return new JsonResult(new { success = false, error = "An unexpected error occurred during bulk deletion" });
        }
    }

    public async Task<IActionResult> OnPostMoveAsync([FromBody] MoveDocumentRequest request)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return new JsonResult(new { success = false, error = "User not authenticated" });
        }

        if (request == null)
        {
            return new JsonResult(new { success = false, error = "Invalid request" });
        }

        // Ensure UserId in request matches authenticated user (owner check will happen in service)
        request.UserId = userId;

        var result = await _documentService.MoveDocumentAsync(request);
        if (!result.Succeeded)
        {
            return new JsonResult(new { success = false, error = result.Error });
        }

        return new JsonResult(new { success = true, document = result.Data });
    }

    public async Task<JsonResult> OnGetFoldersAsync(int projectId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return new JsonResult(new { success = false, error = "User not authenticated" });
        }

        var result = await _folderService.GetProjectFoldersAsync(projectId, userId);
        if (!result.Succeeded)
            return new JsonResult(new { success = false, error = result.Error });

        return new JsonResult(new { success = true, folders = result.Data.Select(f => new { f.Id, f.Name }) });
    }

    public async Task<JsonResult> OnPostBulkMoveAsync([FromBody] BulkMoveRequest request)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return new JsonResult(new { success = false, error = "User not authenticated" });

        if (request?.DocumentIds == null || !request.DocumentIds.Any())
            return new JsonResult(new { success = false, error = "No documents selected" });

        int success = 0;
        foreach (var docId in request.DocumentIds)
        {
            var moveRequest = new MoveDocumentRequest
            {
                DocumentId = docId,
                ProjectId = request.ProjectId,
                FolderId = request.FolderId,
                UserId = userId
            };
            var result = await _documentService.MoveDocumentAsync(moveRequest);
            if (result.Succeeded) success++;
        }
        return new JsonResult(new { success = true, total = request.DocumentIds.Count, successfulMoves = success });
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

    private void SetSortViewData()
    {
        ViewData["CurrentSort"] = Filter.SortBy;
        
        // Set next sort direction for each column
        ViewData["TitleSortDirection"] = Filter.SortBy == "Title" && Filter.SortDirection == "asc" ? "desc" : "asc";
        ViewData["UpdatedAtSortDirection"] = Filter.SortBy == "UpdatedAt" && Filter.SortDirection == "asc" ? "desc" : "asc";
        ViewData["FileSizeSortDirection"] = Filter.SortBy == "FileSize" && Filter.SortDirection == "asc" ? "desc" : "asc";
    }

    public string GetSortUrl(string sortBy, string sortDirection)
    {
        var queryParams = new List<string>();
        
        // Add sorting parameters
        queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
        queryParams.Add($"sortDirection={Uri.EscapeDataString(sortDirection)}");
        
        // Add current filter parameters (excluding existing sort parameters)
        if (!string.IsNullOrEmpty(Filter.SearchTerm))
            queryParams.Add($"Filter.SearchTerm={Uri.EscapeDataString(Filter.SearchTerm)}");
            
        if (!string.IsNullOrEmpty(Filter.FileType))
            queryParams.Add($"Filter.FileType={Uri.EscapeDataString(Filter.FileType)}");
            
        if (Filter.TeamId.HasValue)
            queryParams.Add($"Filter.TeamId={Filter.TeamId.Value}");
            
        if (Filter.CategoryId.HasValue)
            queryParams.Add($"Filter.CategoryId={Filter.CategoryId.Value}");
            
        if (Filter.IncludeTeamDocuments)
            queryParams.Add($"Filter.IncludeTeamDocuments={Filter.IncludeTeamDocuments}");
            
        if (Filter.ProjectId.HasValue)
            queryParams.Add($"Filter.ProjectId={Filter.ProjectId.Value}");
            
        if (Filter.FolderId.HasValue)
            queryParams.Add($"Filter.FolderId={Filter.FolderId.Value}");
            
        // Don't preserve page number when sorting - always reset to page 1 for better UX
            
        if (Filter.PageSize != 10) // Assuming 10 is default
            queryParams.Add($"Filter.PageSize={Filter.PageSize}");
        
        return "?" + string.Join("&", queryParams);
    }

    private async Task LoadFoldersAsync(string userId)
    {
        if (!ProjectId.HasValue || ProjectId.Value <= 0)
        {
            Folders = new List<FolderDto>();
            return;
        }

        var result = await _folderService.GetProjectFoldersAsync(ProjectId.Value, userId);
        if (!result.Succeeded)
        {
            Folders = new List<FolderDto>();
            return;
        }
        // Apply indentation based on folder level for dropdown display
        Folders = result.Data
            .OrderBy(f => f.Path) // Order by path to maintain hierarchy order
            .Select(f => new FolderDto 
            { 
                Id = f.Id, 
                Name = new string('›', f.Level) + " " + f.Name // use chevron character for indentation based on level
            })
            .ToList();
    }
} 