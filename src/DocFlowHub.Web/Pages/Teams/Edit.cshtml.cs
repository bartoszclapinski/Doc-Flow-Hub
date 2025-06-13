using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Web.Extensions;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Web.Pages.Teams
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ITeamService _teamService;

        public EditModel(ITeamService teamService)
        {
            _teamService = teamService;
        }

        [BindProperty]
        [Required(ErrorMessage = "Team name is required")]
        [StringLength(100, ErrorMessage = "Team name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [BindProperty]
        public int TeamId { get; set; }

        public string? TeamName { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            TeamId = id;
            var userId = User.GetUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var result = await _teamService.GetTeamByIdAsync(id);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Error ?? "Team not found";
                return Page();
            }

            var team = result.Data;
            
            // Check if user is the owner
            if (team.OwnerId != userId)
            {
                ErrorMessage = "You can only edit teams you own";
                return RedirectToPage("/Teams/Details", new { id });
            }

            TeamName = team.Name;
            Name = team.Name;
            Description = team.Description;

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
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

            var result = await _teamService.UpdateTeamAsync(TeamId, Name, Description, userId);
            if (result.Succeeded)
            {
                SuccessMessage = "Team updated successfully!";
                TeamName = Name; // Update display name
                return Page();
            }

            ErrorMessage = result.Error ?? "Failed to update team";
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var result = await _teamService.DeleteTeamAsync(TeamId, userId);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Team deleted successfully!";
                return RedirectToPage("/Teams/Index");
            }

            ErrorMessage = result.Error ?? "Failed to delete team";
            return Page();
        }
    }
} 