using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Teams;

/// <summary>New team (decision S13-9). The creator becomes its owner and first admin.</summary>
[Authorize]
public class CreateModel : PageModel
{
    private readonly ITeamService _teamService;

    public CreateModel(ITeamService teamService) => _teamService = teamService;

    [BindProperty]
    [Required(ErrorMessage = "Enter a name")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "The name needs 3 to 100 characters")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "The description can have at most 500 characters")]
    public string? Description { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _teamService.CreateTeamAsync(new CreateTeamRequest
        {
            Name = Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        }, User.GetUserId()!);
        if (!result.Succeeded || result.Data == null)
        {
            ErrorMessage = result.Error ?? "The team could not be created.";
            return Page();
        }

        TempData["ToastSuccess"] = $"Created team \"{result.Data.Name}\"";
        return RedirectToPage("/Teams/Details", new { id = result.Data.Id });
    }
}
