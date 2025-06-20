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

namespace DocFlowHub.Web.Pages.Documents;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentCategoryService _categoryService;
    private readonly ITeamService _teamService;

    public IndexModel(
        IDocumentService documentService,
        IDocumentCategoryService categoryService,
        ITeamService teamService)
    {
        _documentService = documentService;
        _categoryService = categoryService;
        _teamService = teamService;
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
            
        // Don't preserve page number when sorting - always reset to page 1 for better UX
            
        if (Filter.PageSize != 10) // Assuming 10 is default
            queryParams.Add($"Filter.PageSize={Filter.PageSize}");
        
        return "?" + string.Join("&", queryParams);
    }
} 