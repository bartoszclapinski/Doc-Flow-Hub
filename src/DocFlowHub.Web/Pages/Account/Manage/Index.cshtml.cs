using DocFlowHub.Core.Models.Profile;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Account.Manage;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IProfileService _profileService;

    public IndexModel(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public ProfileDto Profile { get; set; }

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

        Profile = profile;
        return Page();
    }
} 