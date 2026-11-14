using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Models.Workspace;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using DocFlowHub.Web.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Folders;

/// <summary>
/// A folder (design 8b, decision S13-4): project tree, subfolders above documents, and the folder's
/// settings (name, description, archived) and delete in the inspector.
/// </summary>
[Authorize]
public class DetailsModel : PageModel
{
    private readonly IWorkspaceBrowserService _workspace;
    private readonly IDocumentBrowserService _documents;
    private readonly IFolderService _folderService;
    private readonly IDemoModeService _demoMode;

    public DetailsModel(IWorkspaceBrowserService workspace, IDocumentBrowserService documents, IFolderService folderService, IDemoModeService demoMode)
    {
        _workspace = workspace;
        _documents = documents;
        _folderService = folderService;
        _demoMode = demoMode;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Enter a name")]
    [StringLength(100, ErrorMessage = "The name can have at most 100 characters")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "The description can have at most 500 characters")]
    public string? Description { get; set; }

    [BindProperty]
    public bool IsArchived { get; set; }

    public FolderView View { get; private set; } = new();
    public DocumentTree Tree { get; private set; } = new();
    public bool IsDemo => _demoMode.IsEnabled;
    public bool SettingsFailed { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;

        Name = View.Folder!.Name;
        Description = View.Folder.Description;
        IsArchived = View.Folder.IsArchived;
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        var folder = View.Folder!;
        var userId = User.GetUserId()!;

        if (!ModelState.IsValid)
        {
            SettingsFailed = true;
            return Page();
        }

        var update = await _folderService.UpdateFolderAsync(Id, new UpdateFolderRequest
        {
            Id = Id,
            Name = Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            ParentFolderId = folder.ParentId,
        }, userId);
        if (!update.Succeeded)
        {
            TempData["ToastError"] = update.Error ?? "The folder could not be saved.";
            return Redirect(WorkspaceLinks.Folder(ProjectId, Id));
        }

        if (IsArchived != folder.IsArchived)
        {
            var state = IsArchived
                ? await _folderService.ArchiveFolderAsync(Id, userId)
                : await _folderService.RestoreFolderAsync(Id, userId);
            if (!state.Succeeded)
            {
                TempData["ToastError"] = state.Error ?? "The folder was saved but its archived state could not be changed.";
                return Redirect(WorkspaceLinks.Folder(ProjectId, Id));
            }
        }

        TempData["ToastSuccess"] = IsArchived && !folder.IsArchived ? "Folder saved and archived" : "Folder saved";
        return Redirect(WorkspaceLinks.Folder(ProjectId, Id));
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        var folder = View.Folder!;

        var result = await _folderService.DeleteFolderAsync(Id, User.GetUserId()!);
        if (!result.Succeeded)
        {
            TempData["ToastError"] = result.Error ?? "The folder could not be deleted.";
            return Redirect(WorkspaceLinks.Folder(ProjectId, Id));
        }

        TempData["ToastSuccess"] = $"Deleted folder \"{folder.Name}\"";
        return Redirect(WorkspaceLinks.Place(ProjectId, folder.ParentId));
    }

    /// <summary>Loads the folder of one of the user's projects, or returns the result to send instead.</summary>
    private async Task<IActionResult?> LoadAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        if (ProjectId <= 0)
        {
            // Older links carry only the folder id.
            var folder = await _folderService.GetFolderByIdAsync(Id, userId);
            if (!folder.Succeeded || folder.Data == null) return NotFound();
            ProjectId = folder.Data.ProjectId;
        }

        var view = await _workspace.GetFolderViewAsync(userId, ProjectId, Id);
        if (!view.Succeeded || view.Data?.Folder == null)
        {
            return NotFound();
        }
        View = view.Data;

        var tree = await _documents.GetTreeAsync(userId);
        if (tree.Succeeded && tree.Data != null) Tree = tree.Data;

        ViewData["StatusPath"] = string.Join(" / ", new[] { View.Project.Name }.Concat(View.Path.Select(p => p.Name)));
        return null;
    }
}
