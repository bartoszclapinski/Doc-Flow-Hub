using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Web.Pages.Admin.Roles;

[Authorize(Roles = "Administrator")]
public class EditModel : PageModel
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoleService _roleService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IRoleService roleService,
        ILogger<EditModel> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _roleService = roleService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel? Input { get; set; }

    public RoleStatisticsViewModel? RoleStats { get; set; }
    public List<UserInRoleViewModel> AssignedUsers { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "Role Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }
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
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role for edit: {RoleId}", id);
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

        if (!ModelState.IsValid)
        {
            await LoadRoleDataById(Input.Id);
            return Page();
        }

        try
        {
            var existingRole = await _roleManager.FindByIdAsync(Input.Id);
            if (existingRole == null)
            {
                return NotFound();
            }

            // Check if new name conflicts with existing role (unless it's the same role)
            if (existingRole.Name != Input.Name)
            {
                var roleWithSameName = await _roleManager.FindByNameAsync(Input.Name);
                if (roleWithSameName != null)
                {
                    ModelState.AddModelError("Input.Name", "A role with this name already exists.");
                    await LoadRoleDataById(Input.Id);
                    return Page();
                }
            }

            // Update the role using the service
            var result = await _roleService.UpdateRoleAsync(Input.Id, Input.Name, Input.Description);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Admin updated role {RoleId}: {RoleName}", Input.Id, Input.Name);
                StatusMessage = $"Role '{Input.Name}' has been updated successfully.";
                return RedirectToPage("./Details", new { id = Input.Id });
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update role.");
                await LoadRoleDataById(Input.Id);
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", Input.Id);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the role. Please try again.");
            await LoadRoleDataById(Input.Id);
            return Page();
        }
    }

    private async Task LoadRoleData(IdentityRole role)
    {
        try
        {
            var roleDto = await _roleService.GetRoleByIdAsync(role.Id);

            Input = new InputModel
            {
                Id = role.Id,
                Name = role.Name ?? "",
                Description = roleDto?.Description
            };

            await LoadRoleStatistics(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role data for role {RoleId}", role.Id);
            Input = null;
        }
    }

    private async Task LoadRoleDataById(string roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role != null)
            {
                await LoadRoleStatistics(role);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role data for role {RoleId}", roleId);
        }
    }

    private async Task LoadRoleStatistics(IdentityRole role)
    {
        try
        {
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

            // Calculate statistics
            var activeUsers = AssignedUsers.Count(u => u.EmailConfirmed);

            RoleStats = new RoleStatisticsViewModel
            {
                UsersCount = AssignedUsers.Count,
                ActiveUsers = activeUsers,
                CreatedAt = DateTime.UtcNow // ASP.NET Identity doesn't track creation date by default
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role statistics for role {RoleId}", role.Id);
            RoleStats = new RoleStatisticsViewModel();
            AssignedUsers = new List<UserInRoleViewModel>();
        }
    }
} 