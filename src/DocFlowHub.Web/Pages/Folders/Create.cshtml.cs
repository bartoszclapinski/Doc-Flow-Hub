using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DocFlowHub.Web.Pages.Folders;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IFolderService _folderService;
    private readonly IProjectService _projectService;

    public CreateModel(IFolderService folderService, IProjectService projectService)
    {
        _folderService = folderService;
        _projectService = projectService;
    }

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ParentFolderId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Folder name is required.")]
    [StringLength(100, ErrorMessage = "Folder name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    public ProjectDto? Project { get; set; }
    public IEnumerable<FolderDto> AvailableParentFolders { get; set; } = new List<FolderDto>();
    public string? ErrorMessage { get; set; }

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

            // Get available parent folders
            var foldersResult = await _folderService.GetFoldersInProjectAsync(
                ProjectId, 
                User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                new FolderFilter { Status = "Active" });

            if (foldersResult.Succeeded)
            {
                AvailableParentFolders = foldersResult.Data.Items;
            }

            // Set parent folder if specified in route
            if (ParentFolderId.HasValue && ParentFolderId.Value > 0)
            {
                var parentFolder = AvailableParentFolders.FirstOrDefault(f => f.Id == ParentFolderId.Value);
                if (parentFolder == null)
                {
                    ErrorMessage = "Specified parent folder not found or access denied.";
                    return RedirectToPage("/Folders/Index", new { projectId = ProjectId });
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred while loading the page.";
            // Log the exception in production
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ProjectId <= 0)
        {
            return RedirectToPage("/Projects/Index");
        }

        try
        {
            // Validate project access
            var projectResult = await _projectService.GetProjectByIdAsync(ProjectId, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!projectResult.Succeeded)
            {
                ErrorMessage = "Project not found or access denied.";
                return RedirectToPage("/Projects/Index");
            }

            Project = projectResult.Data;

            // Validate parent folder if specified
            if (ParentFolderId.HasValue && ParentFolderId.Value > 0)
            {
                var foldersResult = await _folderService.GetFoldersInProjectAsync(
                    ProjectId, 
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    new FolderFilter { Status = "Active" });

                if (foldersResult.Succeeded)
                {
                    AvailableParentFolders = foldersResult.Data.Items;
                    var parentFolder = AvailableParentFolders.FirstOrDefault(f => f.Id == ParentFolderId.Value);
                    if (parentFolder == null)
                    {
                        ModelState.AddModelError("ParentFolderId", "Specified parent folder not found or access denied.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Create folder
            var createRequest = new CreateFolderRequest
            {
                Name = Name.Trim(),
                Description = Description?.Trim(),
                ProjectId = ProjectId,
                ParentFolderId = ParentFolderId
            };

            var result = await _folderService.CreateFolderAsync(createRequest, User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Folder '{Name}' created successfully.";
                return RedirectToPage("/Folders/Index", new { projectId = ProjectId });
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to create folder.";
                return Page();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred while creating the folder.";
            // Log the exception in production
            return Page();
        }
    }
} 