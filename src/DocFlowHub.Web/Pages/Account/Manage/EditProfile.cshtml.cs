using DocFlowHub.Core.Models.Profile;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Web.Pages.Account.Manage;

[Authorize]
public class EditProfileModel : PageModel
{
    private readonly IProfileService _profileService;

    public EditProfileModel(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [StringLength(500, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "Bio")]
        public string Bio { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var profile = await _profileService.GetProfileAsync(userId);
        if (profile == null)
        {
            return RedirectToPage("/Error");
        }

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

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            Bio = Input.Bio
        };

        var updatedProfile = await _profileService.UpdateProfileAsync(userId, updateRequest);
        if (updatedProfile == null)
        {
            ModelState.AddModelError(string.Empty, "Failed to update profile. Please try again.");
            return Page();
        }

        return RedirectToPage("./Index");
    }
} 