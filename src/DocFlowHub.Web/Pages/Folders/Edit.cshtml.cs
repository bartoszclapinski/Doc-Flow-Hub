using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Models.Workspace;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using DocFlowHub.Web.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Folders;

/// <summary>Edit a folder: name, description and the folder it sits in (decision S13-9).</summary>
[Authorize]
public class EditModel : PageModel
{
    private readonly IWorkspaceBrowserService _workspace;
    private readonly IFolderService _folderService;

    public EditModel(IWorkspaceBrowserService workspace, IFolderService folderService)
    {
        _workspace = workspace;
        _folderService = folderService;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Enter a name")]
    [StringLength(100, ErrorMessage = "The name can have at most 100 characters")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "The description can have at most 500 characters")]
    public string? Description { get; set; }

    [BindProperty]
    public int? ParentFolderId { get; set; }

    public FolderDto Folder { get; private set; } = new();
    public ProjectInspector Project { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    /// <summary>Folders it can move into: not itself, not one of its own subfolders, not archived.</summary>
    public IReadOnlyList<ProjectFolderRow> Parents
    {
        get
        {
            var excluded = new HashSet<int> { Folder.Id };
            foreach (var f in Project.Folders)
            {
                if (f.ParentId is int parent && excluded.Contains(parent)) excluded.Add(f.Id);
            }
            return Project.Folders.Where(f => !excluded.Contains(f.Id) && !f.IsArchived).ToList();
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        Name = Folder.Name;
        Description = Folder.Description;
        ParentFolderId = Folder.ParentFolderId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        if (ParentFolderId != null && Parents.All(f => f.Id != ParentFolderId))
        {
            ModelState.AddModelError(nameof(ParentFolderId), "Choose another folder of this project.");
        }
        if (!ModelState.IsValid)
        {
            ErrorMessage = ModelState[nameof(ParentFolderId)]?.Errors.FirstOrDefault()?.ErrorMessage;
            return Page();
        }

        var result = await _folderService.UpdateFolderAsync(Id, new UpdateFolderRequest
        {
            Id = Id,
            Name = Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            ParentFolderId = ParentFolderId,
        }, User.GetUserId()!);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error ?? "The folder could not be saved.";
            return Page();
        }

        TempData["ToastSuccess"] = "Folder saved";
        return Redirect(WorkspaceLinks.Folder(Folder.ProjectId, Id));
    }

    private async Task<IActionResult?> LoadAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }
        var folder = await _folderService.GetFolderByIdAsync(Id, userId);
        if (!folder.Succeeded || folder.Data == null)
        {
            return NotFound();
        }
        Folder = folder.Data;
        var project = await _workspace.GetProjectAsync(userId, Folder.ProjectId);
        if (!project.Succeeded || project.Data == null)
        {
            return NotFound();
        }
        Project = project.Data;
        return null;
    }
}
