using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Web.Pages.Teams;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ITeamService _teamService;

    public CreateModel(ITeamService teamService)
    {
        _teamService = teamService;
    }

    [BindProperty]
    [Required(ErrorMessage = "Team name is required")]
    [StringLength(100, ErrorMessage = "Team name must be between 3 and 100 characters", MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        // Initialize page
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var request = new CreateTeamRequest
        {
            Name = Name,
            Description = Description
        };

        var result = await _teamService.CreateTeamAsync(request, userId);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Team '{Name}' was created successfully!";
            return RedirectToPage("./Details", new { id = result.Data.Id });
        }

        ErrorMessage = result.Error;
        return Page();
    }
} 