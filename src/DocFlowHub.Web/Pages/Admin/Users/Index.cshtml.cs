using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Teams;
using System.Security.Claims;

namespace DocFlowHub.Web.Pages.Admin.Users;

[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDocumentService _documentService;
    private readonly ITeamService _teamService;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        IDocumentService documentService,
        ITeamService teamService)
    {
        _userManager = userManager;
        _documentService = documentService;
        _teamService = teamService;
    }

    public List<UserSummary> Users { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadUsers();
        return Page();
    }

    private async Task LoadUsers()
    {
        try
        {
            var users = await _userManager.Users.ToListAsync();
            Users = new List<UserSummary>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                
                // Get user's document count
                var documentFilter = new DocumentFilter
                {
                    OwnerId = user.Id,
                    PageNumber = 1,
                    PageSize = 1000,
                    IncludeDeleted = false
                };
                var documentsResult = await _documentService.GetAllDocumentsForAdminAsync(documentFilter);
                int documentCount = documentsResult.Succeeded ? documentsResult.Data.TotalItems : 0;

                // Get user's team count
                var teamFilter = new TeamFilter { PageNumber = 1, PageSize = 1000 };
                var teamsResult = await _teamService.GetUserTeamsAsync(user.Id, teamFilter);
                int teamCount = teamsResult.Succeeded ? teamsResult.Data.TotalItems : 0;

                Users.Add(new UserSummary
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    CreatedAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc),
                    LastLoginAt = user.LastLoginAt.HasValue ? DateTime.SpecifyKind(user.LastLoginAt.Value, DateTimeKind.Utc) : null,
                    IsActive = true, // ASP.NET Identity doesn't have built-in active/inactive
                    Roles = userRoles.ToList(),
                    DocumentCount = documentCount,
                    TeamCount = teamCount
                });
            }

            // Sort by creation date, newest first
            Users = Users.OrderByDescending(u => u.CreatedAt).ToList();
        }
        catch (Exception)
        {
            Users = new List<UserSummary>();
        }
    }
}

public class UserSummary
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public int DocumentCount { get; set; }
    public int TeamCount { get; set; }
} 