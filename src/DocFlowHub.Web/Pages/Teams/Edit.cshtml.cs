using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Models.Workspace;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Teams;

/// <summary>Team settings (decision S13-9): rename, describe or delete. The owner only.</summary>
[Authorize]
public class EditModel : PageModel
{
    private readonly IWorkspaceBrowserService _workspace;
    private readonly ITeamService _teamService;

    public EditModel(IWorkspaceBrowserService workspace, ITeamService teamService)
    {
        _workspace = workspace;
        _teamService = teamService;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Enter a name")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "The name needs 3 to 100 characters")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "The description can have at most 500 characters")]
    public string? Description { get; set; }

    public TeamView Team { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        Name = Team.Name;
        Description = Team.Description;
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _teamService.UpdateTeamAsync(Id, Name.Trim(),
            string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(), User.GetUserId()!);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error ?? "The team could not be saved.";
            return Page();
        }

        TempData["ToastSuccess"] = "Team saved";
        return RedirectToPage("/Teams/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;

        var result = await _teamService.DeleteTeamAsync(Id, User.GetUserId()!);
        if (!result.Succeeded)
        {
            TempData["ToastError"] = result.Error ?? "The team could not be deleted.";
            return RedirectToPage(new { id = Id });
        }

        TempData["ToastSuccess"] = $"Deleted team \"{Team.Name}\"";
        return RedirectToPage("/Teams/Index");
    }

    /// <summary>The team, if the user owns it; members see the team page instead.</summary>
    private async Task<IActionResult?> LoadAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }
        var team = await _workspace.GetTeamAsync(userId, Id);
        if (!team.Succeeded || team.Data == null)
        {
            return NotFound();
        }
        if (!team.Data.IsOwner)
        {
            TempData["ToastError"] = "Only the owner can change the team's settings.";
            return RedirectToPage("/Teams/Details", new { id = Id });
        }
        Team = team.Data;
        return null;
    }
}
