using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;

namespace DocFlowHub.Web.Pages.Account.Manage;

[Authorize]
public class UploadProfilePictureModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProfileService _profileService;

    public UploadProfilePictureModel(
        UserManager<ApplicationUser> userManager,
        IProfileService profileService)
    {
        _userManager = userManager;
        _profileService = profileService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();
    public string? CurrentPictureUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Please select a file to upload.")]
        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        CurrentPictureUrl = user.ProfilePictureUrl;
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

        if (Input.ProfilePicture == null)
        {
            ModelState.AddModelError(string.Empty, "Please select a file to upload.");
            return Page();
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
        if (!allowedTypes.Contains(Input.ProfilePicture.ContentType.ToLower()))
        {
            ModelState.AddModelError(string.Empty, "Only JPG, PNG, and GIF files are allowed.");
            return Page();
        }

        // Validate file size (5MB max)
        if (Input.ProfilePicture.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(string.Empty, "File size must be less than 5MB.");
            return Page();
        }

        var result = await _profileService.UpdateProfilePictureAsync(user.Id, Input.ProfilePicture);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update profile picture.");
            return Page();
        }

        TempData["StatusMessage"] = "Your profile picture has been updated.";
        return RedirectToPage();
    }
} 