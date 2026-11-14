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

/// <summary>New folder in one of the user's projects (decision S13-9).</summary>
[Authorize]
public class CreateModel : PageModel
{
    private readonly IWorkspaceBrowserService _workspace;
    private readonly IFolderService _folderService;

    public CreateModel(IWorkspaceBrowserService workspace, IFolderService folderService)
    {
        _workspace = workspace;
        _folderService = folderService;
    }

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ParentFolderId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Enter a name")]
    [StringLength(100, ErrorMessage = "The name can have at most 100 characters")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "The description can have at most 500 characters")]
    public string? Description { get; set; }

    public ProjectInspector Project { get; private set; } = new();
    public IReadOnlyList<ProjectFolderRow> Parents => Project.Folders.Where(f => !f.IsArchived).ToList();
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        if (ParentFolderId != null && Parents.All(f => f.Id != ParentFolderId))
        {
            ParentFolderId = null;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        if (ParentFolderId != null && Parents.All(f => f.Id != ParentFolderId))
        {
            ModelState.AddModelError(nameof(ParentFolderId), "Choose a folder of this project.");
        }
        if (!ModelState.IsValid)
        {
            ErrorMessage = ModelState[nameof(ParentFolderId)]?.Errors.FirstOrDefault()?.ErrorMessage;
            return Page();
        }

        var result = await _folderService.CreateFolderAsync(new CreateFolderRequest
        {
            Name = Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            ProjectId = ProjectId,
            ParentFolderId = ParentFolderId,
        }, User.GetUserId()!);
        if (!result.Succeeded || result.Data == null)
        {
            ErrorMessage = result.Error ?? "The folder could not be created.";
            return Page();
        }

        TempData["ToastSuccess"] = $"Created folder \"{result.Data.Name}\"";
        return Redirect(WorkspaceLinks.Folder(ProjectId, result.Data.Id));
    }

    private async Task<IActionResult?> LoadAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }
        var project = ProjectId > 0 ? await _workspace.GetProjectAsync(userId, ProjectId) : null;
        if (project == null || !project.Succeeded || project.Data == null)
        {
            return RedirectToPage("/Projects/Index");
        }
        Project = project.Data;
        return null;
    }
}
