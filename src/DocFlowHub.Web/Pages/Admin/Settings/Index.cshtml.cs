using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Admin.Settings;

[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAppSettingsAsync()
    {
        // TODO: Implement actual settings update
        TempData["SuccessMessage"] = "Application settings updated successfully.";
        return RedirectToPage();
    }
} 