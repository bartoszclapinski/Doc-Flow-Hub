using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.Role;

namespace DocFlowHub.Web.Pages.Admin.Roles;

[Authorize(Roles = "Administrator")]
public class DetailsModel : PageModel
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoleService _roleService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IRoleService roleService,
        ILogger<DetailsModel> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _roleService = roleService;
        _logger = logger;
    }

    public RoleDetailViewModel? Role { get; set; }
    public List<UserInRoleViewModel> AssignedUsers { get; set; } = new();
    public int ActiveUsers { get; set; }

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
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            await LoadRoleDetails(role);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role details for role {RoleId}", id);
            StatusMessage = "Error loading role details. Please try again.";
            return RedirectToPage("./Index");
        }
    }

    private async Task LoadRoleDetails(IdentityRole role)
    {
        try
        {
            // Load basic role information
            var roleDto = await _roleService.GetRoleByIdAsync(role.Id);
            
            Role = new RoleDetailViewModel
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

            // Calculate active users (email confirmed)
            ActiveUsers = AssignedUsers.Count(u => u.EmailConfirmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role details for role {RoleId}", role.Id);
            Role = null;
            AssignedUsers = new List<UserInRoleViewModel>();
            ActiveUsers = 0;
        }
    }
} 