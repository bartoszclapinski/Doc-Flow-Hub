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

namespace DocFlowHub.Web.Pages.Admin;

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

    // System-wide statistics
    public int TotalUsers { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalTeams { get; set; }
    public int DocumentsThisWeek { get; set; }
    public int TeamsThisWeek { get; set; }
    public long TotalStorageUsed { get; set; } // in bytes
    public List<RecentUserActivity> RecentActivities { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSystemStatistics();
        await LoadRecentActivities();
        return Page();
    }

    private async Task LoadSystemStatistics()
    {
        try
        {
            // Get total users count
            TotalUsers = await _userManager.Users.CountAsync();
            
            // Get admin count
            var adminUsers = await _userManager.GetUsersInRoleAsync("Administrator");
            TotalAdmins = adminUsers.Count;

            // Get all users to calculate system-wide document stats
            var allUsers = await _userManager.Users.ToListAsync();
            TotalDocuments = 0;
            TotalStorageUsed = 0;
            DocumentsThisWeek = 0;

            var weekAgo = DateTime.UtcNow.AddDays(-7);

            // Calculate document statistics
            foreach (var user in allUsers)
            {
                var filter = new DocumentFilter
                {
                    OwnerId = user.Id,
                    PageNumber = 1,
                    PageSize = 1000,
                    IncludeDeleted = false
                };

                var userDocs = await _documentService.GetAllDocumentsForAdminAsync(filter);
                if (userDocs.Succeeded)
                {
                    TotalDocuments += userDocs.Data.TotalItems;
                    TotalStorageUsed += userDocs.Data.Items.Sum(d => d.FileSize);
                    DocumentsThisWeek += userDocs.Data.Items.Count(d => d.CreatedAt >= weekAgo);
                }
            }

            // Calculate team statistics separately to avoid double counting
            await CalculateTeamStatistics(weekAgo);
        }
        catch (Exception)
        {
            // Set default values on error
            TotalUsers = 0;
            TotalAdmins = 0;
            TotalDocuments = 0;
            TotalTeams = 0;
            DocumentsThisWeek = 0;
            TeamsThisWeek = 0;
            TotalStorageUsed = 0;
        }
    }

    private async Task CalculateTeamStatistics(DateTime weekAgo)
    {
        try
        {
            // Use a HashSet to track unique teams by ID to avoid double counting
            var uniqueTeams = new HashSet<int>();
            var recentTeams = new HashSet<int>();
            
            var allUsers = await _userManager.Users.ToListAsync();
            
            // Handle empty user list safely
            if (!allUsers.Any())
            {
                TotalTeams = 0;
                TeamsThisWeek = 0;
                return;
            }

            // Get teams from all users and deduplicate
            foreach (var user in allUsers)
            {
                var teamFilter = new TeamFilter { PageNumber = 1, PageSize = 1000 };
                var userTeams = await _teamService.GetUserTeamsAsync(user.Id, teamFilter);
                
                if (userTeams.Succeeded)
                {
                    foreach (var team in userTeams.Data.Items)
                    {
                        // Add to unique teams set (automatically handles duplicates)
                        uniqueTeams.Add(team.Id);
                        
                        // Track recent teams
                        if (team.CreatedAt >= weekAgo)
                        {
                            recentTeams.Add(team.Id);
                        }
                    }
                }
            }

            TotalTeams = uniqueTeams.Count;
            TeamsThisWeek = recentTeams.Count;
        }
        catch (Exception)
        {
            TotalTeams = 0;
            TeamsThisWeek = 0;
        }
    }

    private async Task LoadRecentActivities()
    {
        try
        {
            RecentActivities = new List<RecentUserActivity>();
            var allUsers = await _userManager.Users.OrderByDescending(u => u.CreatedAt).Take(10).ToListAsync();

            foreach (var user in allUsers)
            {
                RecentActivities.Add(new RecentUserActivity
                {
                    UserName = user.Email ?? "Unknown",
                    Activity = "User registered",
                    Timestamp = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc),
                    Type = "user"
                });
            }

            // Sort by timestamp descending
            RecentActivities = RecentActivities.OrderByDescending(a => a.Timestamp).Take(5).ToList();
        }
        catch (Exception)
        {
            RecentActivities = new List<RecentUserActivity>();
        }
    }
}

public class RecentUserActivity
{
    public string UserName { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty; // "user", "document", "team"
} 