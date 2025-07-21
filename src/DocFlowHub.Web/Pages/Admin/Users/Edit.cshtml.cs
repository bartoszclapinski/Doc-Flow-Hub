using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.Role;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Teams;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Web.Pages.Admin.Users;

[Authorize(Roles = "Administrator")]
public class EditModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoleService _roleService;
    private readonly IDocumentService _documentService;
    private readonly ITeamService _teamService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        UserManager<ApplicationUser> userManager,
        IRoleService roleService,
        IDocumentService documentService,
        ITeamService teamService,
        ILogger<EditModel> logger)
    {
        _userManager = userManager;
        _roleService = roleService;
        _documentService = documentService;
        _teamService = teamService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel? Input { get; set; }

    public List<RoleDto> AvailableRoles { get; set; } = new();
    public UserStatisticsViewModel? UserStats { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Selected Roles")]
        public List<string> SelectedRoles { get; set; } = new();

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Send Password Change Email")]
        public bool SendPasswordChangeEmail { get; set; } = true;
    }

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

            await LoadUserData(user);
            await LoadAvailableRoles();
            await LoadUserStatistics(user);
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user for edit: {UserId}", id);
            StatusMessage = "Error loading user data. Please try again.";
            return RedirectToPage("./Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input == null)
        {
            return NotFound();
        }

        await LoadAvailableRoles();

        var user = await _userManager.FindByIdAsync(Input.Id);
        if (user == null)
        {
            return NotFound();
        }

        await LoadUserStatistics(user);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            bool hasChanges = false;
            var originalEmail = user.Email;

            // Update basic user information
            if (user.Email != Input.Email)
            {
                // Check if email is already in use
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("Input.Email", "This email address is already in use by another user.");
                    return Page();
                }

                user.Email = Input.Email;
                user.UserName = Input.Email;
                user.EmailConfirmed = Input.EmailConfirmed;
                hasChanges = true;
            }

            if (user.FirstName != Input.FirstName)
            {
                user.FirstName = Input.FirstName;
                hasChanges = true;
            }

            if (user.LastName != Input.LastName)
            {
                user.LastName = Input.LastName;
                hasChanges = true;
            }

            if (user.EmailConfirmed != Input.EmailConfirmed)
            {
                user.EmailConfirmed = Input.EmailConfirmed;
                hasChanges = true;
            }

            // Update user information
            if (hasChanges)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }

                _logger.LogInformation("Admin updated user information for {Email}", Input.Email);
            }

            // Handle password change
            if (!string.IsNullOrEmpty(Input.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);
                
                if (passwordResult.Succeeded)
                {
                    _logger.LogInformation("Admin changed password for user {Email}", Input.Email);
                    
                    if (Input.SendPasswordChangeEmail)
                    {
                        // TODO: Send password change notification email
                        _logger.LogInformation("Password change notification would be sent to {Email}", Input.Email);
                    }
                }
                else
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }
            }

            // Handle role changes
            await UpdateUserRoles(user, Input.SelectedRoles);

            StatusMessage = $"User '{Input.Email}' has been updated successfully.";
            return RedirectToPage("./Details", new { id = Input.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", Input.Id);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the user. Please try again.");
            return Page();
        }
    }

    private async Task LoadUserData(ApplicationUser user)
    {
        var userRoles = await _userManager.GetRolesAsync(user);

        Input = new InputModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            SelectedRoles = userRoles.ToList(),
            SendPasswordChangeEmail = true
        };
    }

    private async Task LoadAvailableRoles()
    {
        try
        {
            AvailableRoles = (await _roleService.GetRolesAsync()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading available roles");
            AvailableRoles = new List<RoleDto>();
        }
    }

    private async Task LoadUserStatistics(ApplicationUser user)
    {
        try
        {
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

            UserStats = new UserStatisticsViewModel
            {
                DocumentCount = documentCount,
                TeamCount = teamCount,
                TotalStorageUsed = totalStorageUsed,
                CreatedAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc),
                LastLoginAt = user.LastLoginAt.HasValue ? DateTime.SpecifyKind(user.LastLoginAt.Value, DateTimeKind.Utc) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user statistics for user {UserId}", user.Id);
            UserStats = new UserStatisticsViewModel();
        }
    }

    private async Task UpdateUserRoles(ApplicationUser user, List<string> newRoles)
    {
        try
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            
            // Remove roles that are no longer selected
            var rolesToRemove = currentRoles.Except(newRoles).ToList();
            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    _logger.LogWarning("Failed to remove roles {Roles} from user {Email}: {Errors}", 
                        string.Join(", ", rolesToRemove), user.Email, 
                        string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                }
                else
                {
                    _logger.LogInformation("Removed roles {Roles} from user {Email}", 
                        string.Join(", ", rolesToRemove), user.Email);
                }
            }

            // Add new roles that weren't previously assigned
            var rolesToAdd = newRoles.Except(currentRoles).ToList();
            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    _logger.LogWarning("Failed to add roles {Roles} to user {Email}: {Errors}", 
                        string.Join(", ", rolesToAdd), user.Email, 
                        string.Join(", ", addResult.Errors.Select(e => e.Description)));
                }
                else
                {
                    _logger.LogInformation("Added roles {Roles} to user {Email}", 
                        string.Join(", ", rolesToAdd), user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating roles for user {Email}", user.Email);
        }
    }
}

public class UserStatisticsViewModel
{
    public int DocumentCount { get; set; }
    public int TeamCount { get; set; }
    public long TotalStorageUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
} 