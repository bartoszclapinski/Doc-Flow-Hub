using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace DocFlowHub.Web.Pages.Folders;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IFolderService _folderService;
    private readonly IProjectService _projectService;

    public IndexModel(IFolderService folderService, IProjectService projectService)
    {
        _folderService = folderService;
        _projectService = projectService;
    }

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public FolderFilter Filter { get; set; } = new();

    public PagedResult<FolderDto> Folders { get; set; } = new();
    public ProjectDto? Project { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (ProjectId <= 0)
        {
            return RedirectToPage("/Projects/Index");
        }

        try
        {
            // Get project details
            var projectResult = await _projectService.GetProjectByIdAsync(ProjectId, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!projectResult.Succeeded)
            {
                ErrorMessage = "Project not found or access denied.";
                return RedirectToPage("/Projects/Index");
            }

            Project = projectResult.Data;

            // Get folders for the project
            var foldersResult = await _folderService.GetFoldersInProjectAsync(
                ProjectId, 
                User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                Filter);

            if (foldersResult.Succeeded)
            {
                Folders = foldersResult.Data;
            }
            else
            {
                ErrorMessage = foldersResult.Error ?? "Failed to load folders.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred while loading folders.";
            // Log the exception in production
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var result = await _folderService.DeleteFolderAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Folder deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to delete folder.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while deleting the folder.";
            // Log the exception in production
        }

        return RedirectToPage(new { projectId = ProjectId });
    }

    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        try
        {
            var result = await _folderService.ArchiveFolderAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Folder archived successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to archive folder.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while archiving the folder.";
            // Log the exception in production
        }

        return RedirectToPage(new { projectId = ProjectId });
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        try
        {
            var result = await _folderService.RestoreFolderAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Folder restored successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to restore folder.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while restoring the folder.";
            // Log the exception in production
        }

        return RedirectToPage(new { projectId = ProjectId });
    }

    public async Task<JsonResult> OnPostMoveFolderAsync([FromBody] MoveFolderRequest request)
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if(string.IsNullOrEmpty(userId))
            return new JsonResult(new{success=false,error="User not authenticated"});
        var result = await _folderService.MoveFolderAsync(request.FolderId, request.NewParentFolderId, userId);
        if(!result.Succeeded)
            return new JsonResult(new{success=false,error=result.Error});
        return new JsonResult(new{success=true});
    }
} 