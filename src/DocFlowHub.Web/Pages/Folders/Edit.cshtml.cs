using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DocFlowHub.Web.Pages.Folders;

[Authorize]
public class EditModel : PageModel
{
    private readonly IFolderService _folderService;
    private readonly IProjectService _projectService;

    public EditModel(IFolderService folderService, IProjectService projectService)
    {
        _folderService = folderService;
        _projectService = projectService;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Folder name is required.")]
    [StringLength(100, ErrorMessage = "Folder name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [BindProperty]
    public int? ParentFolderId { get; set; }

    public FolderDto? Folder { get; set; }
    public ProjectDto? Project { get; set; }
    public IEnumerable<FolderDto> AvailableParentFolders { get; set; } = new List<FolderDto>();
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id <= 0)
        {
            return RedirectToPage("/Projects/Index");
        }

        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            // Get folder details
            var folderResult = await _folderService.GetFolderByIdAsync(Id, userId);
            if (!folderResult.Succeeded)
            {
                ErrorMessage = "Folder not found or access denied.";
                return RedirectToPage("/Projects/Index");
            }

            Folder = folderResult.Data;

            // Pre-populate form fields
            Name = Folder.Name;
            Description = Folder.Description;
            ParentFolderId = Folder.ParentFolderId;

            // Get project details
            var projectResult = await _projectService.GetProjectByIdAsync(Folder.ProjectId, userId);
            if (projectResult.Succeeded)
            {
                Project = projectResult.Data;
            }

            // Get available parent folders (excluding current folder and its descendants)
            var foldersResult = await _folderService.GetFoldersInProjectAsync(
                Folder.ProjectId, 
                userId,
                new FolderFilter { Status = "Active" });

            if (foldersResult.Succeeded)
            {
                // Filter out current folder and its descendants to prevent circular references
                AvailableParentFolders = foldersResult.Data.Items.Where(f => 
                    f.Id != Folder.Id && 
                    !IsDescendant(f, Folder.Id, foldersResult.Data.Items));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred while loading the folder.";
            // Log the exception in production
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Id <= 0)
        {
            return RedirectToPage("/Projects/Index");
        }

        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            // Validate folder access
            var folderResult = await _folderService.GetFolderByIdAsync(Id, userId);
            if (!folderResult.Succeeded)
            {
                ErrorMessage = "Folder not found or access denied.";
                return RedirectToPage("/Projects/Index");
            }

            Folder = folderResult.Data;

            // Get project details
            var projectResult = await _projectService.GetProjectByIdAsync(Folder.ProjectId, userId);
            if (projectResult.Succeeded)
            {
                Project = projectResult.Data;
            }

            // Validate parent folder if specified
            if (ParentFolderId.HasValue && ParentFolderId.Value > 0)
            {
                var foldersResult = await _folderService.GetFoldersInProjectAsync(
                    Folder.ProjectId, 
                    userId,
                    new FolderFilter { Status = "Active" });

                if (foldersResult.Succeeded)
                {
                    AvailableParentFolders = foldersResult.Data.Items.Where(f => 
                        f.Id != Folder.Id && 
                        !IsDescendant(f, Folder.Id, foldersResult.Data.Items));

                    var parentFolder = AvailableParentFolders.FirstOrDefault(f => f.Id == ParentFolderId.Value);
                    if (parentFolder == null)
                    {
                        ModelState.AddModelError("ParentFolderId", "Specified parent folder not found or would create a circular reference.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Update folder
            var updateRequest = new UpdateFolderRequest
            {
                Id = Id,
                Name = Name.Trim(),
                Description = Description?.Trim(),
                ParentFolderId = ParentFolderId
            };

            var result = await _folderService.UpdateFolderAsync(Id, updateRequest, userId);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Folder '{Name}' updated successfully.";
                return RedirectToPage("/Folders/Details", new { id = Id });
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to update folder.";
                return Page();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred while updating the folder.";
            // Log the exception in production
            return Page();
        }
    }

    private bool IsDescendant(FolderDto folder, int ancestorId, IEnumerable<FolderDto> allFolders)
    {
        if (folder.ParentFolderId == ancestorId)
        {
            return true;
        }

        if (folder.ParentFolderId.HasValue)
        {
            var parent = allFolders.FirstOrDefault(f => f.Id == folder.ParentFolderId.Value);
            if (parent != null)
            {
                return IsDescendant(parent, ancestorId, allFolders);
            }
        }

        return false;
    }
} 