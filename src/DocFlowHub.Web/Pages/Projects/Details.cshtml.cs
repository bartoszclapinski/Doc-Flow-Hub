using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Models.Workspace;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Projects;

/// <summary>
/// A project's root (design 8b, decision S13-4): project tree, root folders above root documents and
/// the project inspector. Archive / restore / delete post to Projects/Index.
/// </summary>
[Authorize]
public class DetailsModel : PageModel
{
    private readonly IWorkspaceBrowserService _workspace;
    private readonly IDocumentBrowserService _documents;
    private readonly IDemoModeService _demoMode;

    public DetailsModel(IWorkspaceBrowserService workspace, IDocumentBrowserService documents, IDemoModeService demoMode)
    {
        _workspace = workspace;
        _documents = documents;
        _demoMode = demoMode;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public FolderView View { get; private set; } = new();
    public DocumentTree Tree { get; private set; } = new();
    public bool IsDemo => _demoMode.IsEnabled;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Projects are owner-only: someone else's project is simply not found.
        var view = await _workspace.GetFolderViewAsync(userId, Id, null);
        if (!view.Succeeded || view.Data == null)
        {
            return NotFound();
        }
        View = view.Data;

        var tree = await _documents.GetTreeAsync(userId);
        if (tree.Succeeded && tree.Data != null) Tree = tree.Data;

        ViewData["StatusPath"] = View.Project.Name;
        return Page();
    }
}
