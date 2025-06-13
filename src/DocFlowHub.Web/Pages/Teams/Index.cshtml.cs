using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;

namespace DocFlowHub.Web.Pages.Teams;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ITeamService _teamService;

    public IndexModel(ITeamService teamService)
    {
        _teamService = teamService;
    }

    public PagedResult<TeamDto> Teams { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public TeamFilter Filter { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var result = await _teamService.GetUserTeamsAsync(userId, Filter);
        if (result.Succeeded)
        {
            Teams = result.Data;
        }
        else
        {
            ErrorMessage = result.Error;
        }

        return Page();
    }
}