using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Workspace;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Teams;

/// <summary>
/// A team (design 3d, decision S13-7): members with roles, and the real permission rules. Admins (the
/// owner is one) add and remove members and change roles; only the owner edits or deletes the team.
/// </summary>
[Authorize]
public class DetailsModel : PageModel
{
    private readonly IWorkspaceBrowserService _workspace;
    private readonly ITeamService _teamService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDemoModeService _demoMode;

    public DetailsModel(IWorkspaceBrowserService workspace, ITeamService teamService, UserManager<ApplicationUser> userManager, IDemoModeService demoMode)
    {
        _workspace = workspace;
        _teamService = teamService;
        _userManager = userManager;
        _demoMode = demoMode;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public TeamView Team { get; private set; } = new();
    public bool IsDemo => _demoMode.IsEnabled;
    public bool CanManageMembers => Team.IsAdmin && !IsDemo;
    public bool CanEditTeam => Team.IsOwner && !IsDemo;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var team = await _workspace.GetTeamAsync(userId, Id);
        if (!team.Succeeded || team.Data == null)
        {
            var exists = await _teamService.GetTeamByIdAsync(Id);
            return exists.Succeeded ? Forbid() : NotFound();
        }
        Team = team.Data;
        ViewData["StatusPath"] = $"teams / {Team.Name}";
        return Page();
    }

    public async Task<IActionResult> OnPostAddMemberAsync(string? email, string? role)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            return Back(error: "Enter the e-mail address of the person to add.");
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user == null)
        {
            return Back(error: $"There is no DocFlowHub account for {email.Trim()}. Ask them to register first.");
        }

        var add = await _teamService.AddMemberToTeamAsync(Id, user.Id, userId);
        if (!add.Succeeded)
        {
            return Back(error: add.Error ?? "The member could not be added.");
        }

        var name = $"{user.FirstName} {user.LastName}".Trim();
        name = name.Length > 0 ? name : user.Email ?? email;
        if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            var promote = await _teamService.UpdateMemberRoleAsync(Id, user.Id, TeamRole.Admin, userId);
            if (!promote.Succeeded)
            {
                return Back(error: $"Added {name} as a member, but the role could not be changed: {promote.Error}");
            }
            return Back(success: $"Added {name} as an admin");
        }
        return Back(success: $"Added {name}");
    }

    public async Task<IActionResult> OnPostUpdateMemberRoleAsync(string memberUserId, string? newRole)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var role = string.Equals(newRole, "admin", StringComparison.OrdinalIgnoreCase) ? TeamRole.Admin : TeamRole.Member;
        var result = await _teamService.UpdateMemberRoleAsync(Id, memberUserId, role, userId);
        return result.Succeeded
            ? Back(success: role == TeamRole.Admin ? "Role changed to Admin" : "Role changed to Member")
            : Back(error: result.Error ?? "The role could not be changed.");
    }

    public async Task<IActionResult> OnPostRemoveMemberAsync(string memberUserId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _teamService.RemoveMemberFromTeamAsync(Id, memberUserId, userId);
        return result.Succeeded
            ? Back(success: "Member removed")
            : Back(error: result.Error ?? "The member could not be removed.");
    }

    private IActionResult Back(string? success = null, string? error = null)
    {
        if (success != null) TempData["ToastSuccess"] = success;
        if (error != null) TempData["ToastError"] = error;
        return RedirectToPage(new { id = Id });
    }
}
