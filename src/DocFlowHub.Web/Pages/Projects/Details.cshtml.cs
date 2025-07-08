using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.Threading.Tasks;
using System.Security.Claims;

namespace DocFlowHub.Web.Pages.Projects;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IProjectService _projectService;
    private readonly IDocumentService _documentService;
    private readonly IFolderService _folderService;

    public DetailsModel(
        IProjectService projectService,
        IDocumentService documentService,
        IFolderService folderService)
    {
        _projectService = projectService;
        _documentService = documentService;
        _folderService = folderService;
    }

    public ProjectDto? Project { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsOwner { get; set; }

    // Quick stats for the project
    public int TotalDocuments { get; set; }
    public int TotalFolders { get; set; }
    public long TotalSize { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Get project details
        var projectResult = await _projectService.GetProjectByIdAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!projectResult.Succeeded)
        {
            ErrorMessage = projectResult.Error ?? "Project not found.";
            return Page();
        }

        Project = projectResult.Data;
        if (Project == null)
        {
            ErrorMessage = "Project not found.";
            return Page();
        }

        // Check if user is the owner
        IsOwner = Project.OwnerId == userId;

        // Load project statistics from the existing data
        TotalDocuments = Project.DocumentCount;
        TotalFolders = Project.FolderCount;
        TotalSize = 0; // Will be calculated when documents are implemented

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var result = await _projectService.DeleteProjectAsync(id, userId);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        TempData["SuccessMessage"] = "Project has been deleted successfully.";
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var result = await _projectService.ArchiveProjectAsync(id, userId);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        TempData["SuccessMessage"] = "Project has been archived successfully.";
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var result = await _projectService.RestoreProjectAsync(id, userId);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        TempData["SuccessMessage"] = "Project has been restored successfully.";
        return RedirectToPage("./Index");
    }


} 