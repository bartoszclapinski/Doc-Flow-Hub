using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Account.Manage;

/// <summary>Old address: the picture is changed on the profile page now (decision S14-9).</summary>
[Authorize]
public class UploadProfilePictureModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Account/Manage/Index");
}
