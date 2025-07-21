using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Web.Pages.Admin.Roles;

[Authorize(Roles = "Administrator")]
public class DeleteModel : PageModel
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoleService _roleService;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IRoleService roleService,
        ILogger<DeleteModel> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _roleService = roleService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel? Input { get; set; }

    public RoleDeleteViewModel? Role { get; set; }
    public List<UserInRoleViewModel> AssignedUsers { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must type the role name to confirm deletion.")]
        [Display(Name = "Confirmation Text")]
        public string ConfirmationText { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must confirm understanding before proceeding.")]
        [Display(Name = "Confirm Understanding")]
        public bool ConfirmUnderstanding { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            await LoadRoleData(role);
            
            Input = new InputModel { Id = role.Id };
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role for deletion: {RoleId}", id);
            StatusMessage = "Error loading role data. Please try again.";
            return RedirectToPage("./Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input == null)
        {
            return NotFound();
        }

        try
        {
            var role = await _roleManager.FindByIdAsync(Input.Id);
            if (role == null)
            {
                return NotFound();
            }

            await LoadRoleData(role);

            // Validate confirmation text matches role name exactly
            if (Input.ConfirmationText != Role?.Name)
            {
                ModelState.AddModelError("Input.ConfirmationText", 
                    $"The confirmation text must exactly match the role name: {Role?.Name}");
            }

            // Additional safety checks
            if (Role?.Name == "Administrator")
            {
                ModelState.AddModelError(string.Empty, 
                    "The Administrator role cannot be deleted for security reasons.");
            }

            // Check for assigned users (this should be caught by the UI, but double-check)
            if (AssignedUsers.Any())
            {
                ModelState.AddModelError(string.Empty, 
                    $"Cannot delete role. {AssignedUsers.Count} user(s) are still assigned to this role.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Attempt to delete the role using the service
            var result = await _roleService.DeleteRoleAsync(Input.Id);
            
            if (result.Succeeded)
            {
                _logger.LogWarning("Admin deleted role {RoleId}: {RoleName}", Input.Id, Role?.Name);
                StatusMessage = $"Role '{Role?.Name}' has been permanently deleted.";
                return RedirectToPage("./Index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to delete role.");
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", Input.Id);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while deleting the role. Please try again.");
            await LoadRoleDataById(Input.Id);
            return Page();
        }
    }

    private async Task LoadRoleData(IdentityRole role)
    {
        try
        {
            var roleDto = await _roleService.GetRoleByIdAsync(role.Id);

            Role = new RoleDeleteViewModel
            {
                Id = role.Id,
                Name = role.Name ?? "",
                Description = roleDto?.Description,
                CreatedAt = DateTime.UtcNow, // ASP.NET Identity doesn't track creation date by default
                UsersCount = roleDto?.UsersCount ?? 0
            };

            // Load assigned users
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? "");
            AssignedUsers = usersInRole.Select(u => new UserInRoleViewModel
            {
                Id = u.Id,
                Email = u.Email ?? "",
                FirstName = u.FirstName,
                LastName = u.LastName,
                EmailConfirmed = u.EmailConfirmed,
                CreatedAt = DateTime.SpecifyKind(u.CreatedAt, DateTimeKind.Utc)
            }).OrderBy(u => u.Email).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role data for role {RoleId}", role.Id);
            Role = null;
            AssignedUsers = new List<UserInRoleViewModel>();
        }
    }

    private async Task LoadRoleDataById(string roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role != null)
            {
                await LoadRoleData(role);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role data for role {RoleId}", roleId);
            Role = null;
            AssignedUsers = new List<UserInRoleViewModel>();
        }
    }
} 