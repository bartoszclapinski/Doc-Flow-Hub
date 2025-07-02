using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocFlowHub.Web.Pages.Projects;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IProjectService _projectService;

    public IndexModel(IProjectService projectService)
    {
        _projectService = projectService;
    }

    public PagedResult<ProjectDto> Projects { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public ProjectFilter Filter { get; set; } = new();

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
            Filter.SortDescending = SortDirection == "desc";
        }

        // Set ViewData for sorting indicators
        SetSortViewData();

        // Get user's projects with filtering and pagination
        var projectsResult = await _projectService.GetUserProjectsAsync(userId, Filter);
        if (!projectsResult.Succeeded)
        {
            ErrorMessage = projectsResult.Error;
            return Page();
        }

        Projects = projectsResult.Data;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int projectId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get project details first to check ownership
        var projectResult = await _projectService.GetProjectByIdAsync(projectId);
        if (!projectResult.Succeeded)
        {
            TempData["ErrorMessage"] = projectResult.Error;
            return RedirectToPage();
        }

        var project = projectResult.Data;

        // Verify user owns this project (only owners can delete)
        if (project.OwnerId != userId)
        {
            TempData["ErrorMessage"] = "You can only delete projects that you own.";
            return RedirectToPage();
        }

        // Delete the project
        var deleteResult = await _projectService.DeleteProjectAsync(projectId, userId);
        if (!deleteResult.Succeeded)
        {
            TempData["ErrorMessage"] = deleteResult.Error;
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"Project '{project.Name}' has been successfully deleted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostArchiveAsync(int projectId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _projectService.ArchiveProjectAsync(projectId, userId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = "Project has been archived successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRestoreAsync(int projectId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _projectService.RestoreProjectAsync(projectId, userId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = "Project has been restored successfully.";
        return RedirectToPage();
    }

    private void SetSortViewData()
    {
        ViewData["NameSortClass"] = Filter.SortBy == "name" && !Filter.SortDescending ? "sort-asc" : 
                                   Filter.SortBy == "name" && Filter.SortDescending ? "sort-desc" : "";
        ViewData["CreatedSortClass"] = Filter.SortBy == "createdat" && !Filter.SortDescending ? "sort-asc" : 
                                      Filter.SortBy == "createdat" && Filter.SortDescending ? "sort-desc" : "";
        ViewData["UpdatedSortClass"] = Filter.SortBy == "updatedat" && !Filter.SortDescending ? "sort-asc" : 
                                      Filter.SortBy == "updatedat" && Filter.SortDescending ? "sort-desc" : "";
    }

    public string GetSortUrl(string sortBy, string sortDirection)
    {
        return Url.Page("/Projects/Index", new { 
            Filter.SearchTerm, 
            Filter.IsActive,
            Filter.CreatedAfter,
            Filter.CreatedBefore,
            Filter.PageNumber,
            Filter.PageSize,
            SortBy = sortBy, 
            SortDirection = sortDirection 
        }) ?? "";
    }
} 