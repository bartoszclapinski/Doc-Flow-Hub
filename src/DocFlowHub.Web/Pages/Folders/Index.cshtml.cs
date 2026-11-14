using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using DocFlowHub.Web.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Folders;

/// <summary>
/// The old folder tree page (decision S13-5): the tree is now the project pages' tool window, so a GET
/// opens the project. It keeps the JSON handler the tree's drag-and-drop uses to move a folder.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IFolderService _folderService;

    public IndexModel(IFolderService folderService) => _folderService = folderService;

    public IActionResult OnGet(int? projectId) =>
        projectId is int id && id > 0 ? Redirect(WorkspaceLinks.Project(id)) : RedirectToPage("/Projects/Index");

    public async Task<JsonResult> OnPostMoveFolderAsync([FromBody] MoveFolderRequest? request)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return new JsonResult(new { success = false, message = "You are signed out. Sign in and try again." });
        }
        if (request == null || request.FolderId <= 0)
        {
            return new JsonResult(new { success = false, message = "Choose a folder to move." });
        }

        var result = await _folderService.MoveFolderAsync(request.FolderId, request.NewParentFolderId, userId);
        return result.Succeeded
            ? new JsonResult(new { success = true })
            : new JsonResult(new { success = false, message = result.Error ?? "The folder could not be moved." });
    }
}
