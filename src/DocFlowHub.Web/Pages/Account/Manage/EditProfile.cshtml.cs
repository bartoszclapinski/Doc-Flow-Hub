using DocFlowHub.Core.Models.Profile;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using DocFlowHub.Core.Identity;

namespace DocFlowHub.Web.Pages.Account.Manage;

[Authorize]
public class EditProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProfileService _profileService;

    public EditProfileModel(
        UserManager<ApplicationUser> userManager,
        IProfileService profileService)
    {
        _userManager = userManager;
        _profileService = profileService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "Bio")]
        public string? Bio { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var profile = await _profileService.GetProfileAsync(user.Id);
        Input = new InputModel
        {
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Bio = profile.Bio
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        try
        {
            var request = new UpdateProfileRequest
            {
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Bio = Input.Bio
            };

            await _profileService.UpdateProfileAsync(user.Id, request);
            TempData["StatusMessage"] = "Your profile has been updated.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
} 