using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Account.Manage;

/// <summary>Old address: name and bio are edited on the profile page now (decision S14-9).</summary>
[Authorize]
public class EditProfileModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Account/Manage/Index");
}
