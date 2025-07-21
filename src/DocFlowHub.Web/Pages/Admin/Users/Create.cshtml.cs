using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.Role;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Web.Pages.Admin.Users;

[Authorize(Roles = "Administrator")]
public class CreateModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoleService _roleService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        UserManager<ApplicationUser> userManager,
        IRoleService roleService,
        ILogger<CreateModel> logger)
    {
        _userManager = userManager;
        _roleService = roleService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<RoleDto> AvailableRoles { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
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

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Selected Roles")]
        public List<string> SelectedRoles { get; set; } = new();

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; } = true;

        [Display(Name = "Send Welcome Email")]
        public bool SendWelcomeEmail { get; set; } = false;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAvailableRoles();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAvailableRoles();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Check if user with this email already exists
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", "A user with this email address already exists.");
                return Page();
            }

            // Create the user
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                EmailConfirmed = Input.EmailConfirmed,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Admin created a new user account: {Email}", Input.Email);

                // Assign roles if any were selected
                if (Input.SelectedRoles.Any())
                {
                    var roleAssignmentResult = await _userManager.AddToRolesAsync(user, Input.SelectedRoles);
                    if (!roleAssignmentResult.Succeeded)
                    {
                        _logger.LogWarning("Failed to assign roles to user {Email}: {Errors}", 
                            Input.Email, string.Join(", ", roleAssignmentResult.Errors.Select(e => e.Description)));
                        
                        // Add warning but don't fail the creation
                        StatusMessage = $"User created successfully, but some roles could not be assigned: {string.Join(", ", roleAssignmentResult.Errors.Select(e => e.Description))}";
                    }
                    else
                    {
                        _logger.LogInformation("Successfully assigned roles {Roles} to user {Email}", 
                            string.Join(", ", Input.SelectedRoles), Input.Email);
                    }
                }

                // Send welcome email if requested
                if (Input.SendWelcomeEmail)
                {
                    try
                    {
                        await SendWelcomeEmail(user);
                        _logger.LogInformation("Welcome email sent to {Email}", Input.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email to {Email}", Input.Email);
                        StatusMessage = "User created successfully, but welcome email could not be sent.";
                    }
                }

                if (string.IsNullOrEmpty(StatusMessage))
                {
                    StatusMessage = $"User '{Input.Email}' has been created successfully.";
                }

                return RedirectToPage("./Index");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                
                _logger.LogWarning("Failed to create user {Email}: {Errors}", 
                    Input.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email}", Input.Email);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the user. Please try again.");
        }

        return Page();
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

    private async Task SendWelcomeEmail(ApplicationUser user)
    {
        // TODO: Implement welcome email functionality
        // This would typically use an email service to send credentials
        // For now, just log the action
        _logger.LogInformation("Welcome email would be sent to {Email} with login instructions", user.Email);
        
        // In a real implementation, you would:
        // 1. Generate a temporary login link or provide login instructions
        // 2. Use an email service (SendGrid, SMTP, etc.) to send the email
        // 3. Include user credentials and next steps
        
        await Task.CompletedTask; // Placeholder for actual email sending
    }
} 