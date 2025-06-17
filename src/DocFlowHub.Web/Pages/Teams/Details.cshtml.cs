using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using DocFlowHub.Core.Identity;

namespace DocFlowHub.Web.Pages.Teams;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ITeamService _teamService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(ITeamService teamService, UserManager<ApplicationUser> userManager)
    {
        _teamService = teamService;
        _userManager = userManager;
    }

    public TeamDto Team { get; set; } = null!;
    public PagedResult<TeamMemberDto> Members { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty]
    public string? NewMemberEmail { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Load team details
        var teamResult = await _teamService.GetTeamByIdAsync(Id);
        if (!teamResult.Succeeded)
        {
            ErrorMessage = teamResult.Error;
            return NotFound();
        }

        Team = teamResult.Data;

        // Authorization check: Verify user is team owner or member
        var hasAccess = await CanUserAccessTeamAsync(userId, Id);
        if (!hasAccess)
        {
            return Forbid();
        }

        // Load team members
        var membersResult = await _teamService.GetTeamMembersAsync(Id);
        if (membersResult.Succeeded)
        {
            Members = membersResult.Data;
        }

        // Check for success/error messages from redirect
        if (TempData["SuccessMessage"] != null)
        {
            SuccessMessage = TempData["SuccessMessage"]?.ToString();
        }
        if (TempData["ErrorMessage"] != null)
        {
            ErrorMessage = TempData["ErrorMessage"]?.ToString();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddMemberAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(NewMemberEmail))
        {
            TempData["ErrorMessage"] = "Email address is required";
            return RedirectToPage("Details", new { id = Id });
        }

        try
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(NewMemberEmail.Trim());
            if (user == null)
            {
                TempData["ErrorMessage"] = $"No user found with email address: {NewMemberEmail}";
                return RedirectToPage("Details", new { id = Id });
            }

            // Add user to team
            var result = await _teamService.AddMemberToTeamAsync(Id, user.Id, userId);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Successfully added {user.UserName ?? user.Email} to the team!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Error;
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while adding the member. Please try again.";
        }

        return RedirectToPage("Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveMemberAsync(string memberUserId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(memberUserId))
        {
            TempData["ErrorMessage"] = "Invalid member specified";
            return RedirectToPage("Details", new { id = Id });
        }

        var result = await _teamService.RemoveMemberFromTeamAsync(Id, memberUserId, userId);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Member removed successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error;
        }

        return RedirectToPage("Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostUpdateMemberRoleAsync(string memberUserId, TeamRole newRole)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(memberUserId))
        {
            TempData["ErrorMessage"] = "Invalid member specified";
            return RedirectToPage("Details", new { id = Id });
        }

        var result = await _teamService.UpdateMemberRoleAsync(Id, memberUserId, newRole, userId);
        if (result.Succeeded)
        {
            var roleText = newRole == TeamRole.Admin ? "Admin" : "Member";
            TempData["SuccessMessage"] = $"Member role updated to {roleText} successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error;
        }

        return RedirectToPage("Details", new { id = Id });
    }

    private async Task<bool> CanUserAccessTeamAsync(string userId, int teamId)
    {
        // Check if user is team owner
        var teamResult = await _teamService.GetTeamByIdAsync(teamId);
        if (teamResult.Succeeded && teamResult.Data.OwnerId == userId)
        {
            return true;
        }

        // Check if user is team member
        var membersResult = await _teamService.GetTeamMembersAsync(teamId);
        if (membersResult.Succeeded)
        {
            return membersResult.Data.Items.Any(m => m.UserId == userId);
        }

        return false;
    }
} 