using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Profile;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Account.Manage;

/// <summary>
/// Profile (design 8e, decision S14-9): picture, name, bio and e-mail on one page. It replaces the
/// read-only profile, EditProfile and UploadProfilePicture pages (those URLs redirect here).
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IProfileService _profileService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(IProfileService profileService, UserManager<ApplicationUser> userManager)
    {
        _profileService = profileService;
        _userManager = userManager;
    }

    [BindProperty]
    [Required(ErrorMessage = "Enter your first name")]
    [StringLength(50, ErrorMessage = "At most 50 characters")]
    public string FirstName { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Enter your last name")]
    [StringLength(50, ErrorMessage = "At most 50 characters")]
    public string LastName { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "At most 500 characters")]
    public string? Bio { get; set; }

    public ProfileDto Profile { get; private set; } = new();
    public bool EmailConfirmed { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        FirstName = Profile.FirstName;
        LastName = Profile.LastName;
        Bio = Profile.Bio;
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _profileService.UpdateProfileAsync(Profile.Id, new UpdateProfileRequest
        {
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            Bio = string.IsNullOrWhiteSpace(Bio) ? null : Bio.Trim(),
        });
        TempData["ToastSuccess"] = "Profile saved";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPictureAsync(IFormFile? picture)
    {
        var load = await LoadAsync();
        if (load != null) return load;
        if (picture == null || picture.Length == 0)
        {
            TempData["ToastError"] = "Choose a picture.";
            return RedirectToPage();
        }

        // The service checks type (JPG, PNG, GIF), content and size (5 MB).
        var result = await _profileService.UpdateProfilePictureAsync(Profile.Id, picture);
        if (result.Succeeded) TempData["ToastSuccess"] = "Picture updated";
        else TempData["ToastError"] = result.Error ?? "The picture could not be saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemovePictureAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        var result = await _profileService.DeleteProfilePictureAsync(Profile.Id);
        if (result.Succeeded) TempData["ToastSuccess"] = "Picture removed";
        else TempData["ToastError"] = result.Error ?? "The picture could not be removed.";
        return RedirectToPage();
    }

    private async Task<IActionResult?> LoadAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }
        Profile = await _profileService.GetProfileAsync(userId);
        var user = await _userManager.FindByIdAsync(userId);
        EmailConfirmed = user?.EmailConfirmed == true;
        return null;
    }
}
