using DocFlowHub.Core.Models.Workspace;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Teams;

/// <summary>Teams (no mock, decision S13-6): the user's teams in a table; a row opens the team.</summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IWorkspaceBrowserService _workspace;

    public IndexModel(IWorkspaceBrowserService workspace) => _workspace = workspace;

    public IReadOnlyList<TeamRow> Teams { get; private set; } = Array.Empty<TeamRow>();
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var result = await _workspace.GetTeamsAsync(userId);
        if (result.Succeeded && result.Data != null) Teams = result.Data;
        else ErrorMessage = result.Error ?? "The teams could not be loaded.";
        return Page();
    }
}
