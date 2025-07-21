using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Teams;

namespace DocFlowHub.Web.Pages.Admin.Users;

[Authorize(Roles = "Administrator")]
public class DetailsModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDocumentService _documentService;
    private readonly ITeamService _teamService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        UserManager<ApplicationUser> userManager,
        IDocumentService documentService,
        ITeamService teamService,
        ILogger<DetailsModel> logger)
    {
        _userManager = userManager;
        _documentService = documentService;
        _teamService = teamService;
        _logger = logger;
    }

    public UserDetailViewModel? UserDetail { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await LoadUserDetails(user);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user details for user {UserId}", id);
            StatusMessage = "Error loading user details. Please try again.";
            return RedirectToPage("./Index");
        }
    }

    private async Task LoadUserDetails(ApplicationUser user)
    {
        try
        {
            // Get user roles
            var userRoles = await _userManager.GetRolesAsync(user);

            // Get user's document count and storage usage
            var documentFilter = new DocumentFilter
            {
                OwnerId = user.Id,
                PageNumber = 1,
                PageSize = 1000,
                IncludeDeleted = false
            };
            var documentsResult = await _documentService.GetAllDocumentsForAdminAsync(documentFilter);
            int documentCount = documentsResult.Succeeded ? documentsResult.Data.TotalItems : 0;
            long totalStorageUsed = documentsResult.Succeeded ? documentsResult.Data.Items.Sum(d => d.FileSize) : 0;

            // Get user's team count
            var teamFilter = new TeamFilter { PageNumber = 1, PageSize = 1000 };
            var teamsResult = await _teamService.GetUserTeamsAsync(user.Id, teamFilter);
            int teamCount = teamsResult.Succeeded ? teamsResult.Data.TotalItems : 0;

            UserDetail = new UserDetailViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc),
                LastLoginAt = user.LastLoginAt.HasValue ? DateTime.SpecifyKind(user.LastLoginAt.Value, DateTimeKind.Utc) : null,
                EmailConfirmed = user.EmailConfirmed,
                IsActive = true, // ASP.NET Identity doesn't have built-in active/inactive
                Roles = userRoles.ToList(),
                DocumentCount = documentCount,
                TeamCount = teamCount,
                TotalStorageUsed = totalStorageUsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user details for user {UserId}", user.Id);
            UserDetail = null;
        }
    }
}

public class UserDetailViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public int DocumentCount { get; set; }
    public int TeamCount { get; set; }
    public long TotalStorageUsed { get; set; }
    
    public string FullName => $"{FirstName} {LastName}".Trim();
} 