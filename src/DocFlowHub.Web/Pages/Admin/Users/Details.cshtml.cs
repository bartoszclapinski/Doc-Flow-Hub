using DocFlowHub.Core.Models.Admin.Browser;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Admin.Users;

/// <summary>
/// One user for admins (decision S14-4): status and roles, usage from one aggregate query, teams,
/// recent recorded actions, quick actions and a message. The old page loaded up to 1000 documents to
/// count them and showed every user as active.
/// </summary>
[Authorize(Roles = "Administrator")]
public class DetailsModel : PageModel
{
    private readonly IAdminBrowserService _admin;
    private readonly IUserCommunicationService _messages;
    private readonly IDemoModeService _demoMode;

    public DetailsModel(IAdminBrowserService admin, IUserCommunicationService messages, IDemoModeService demoMode)
    {
        _admin = admin;
        _messages = messages;
        _demoMode = demoMode;
    }

    public AdminUserDetails Details { get; private set; } = new();
    public bool IsDemo => _demoMode.IsEnabled;
    public bool IsMe => Details.Id == User.GetUserId();

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }
        var details = await _admin.GetUserDetailsAsync(id);
        if (!details.Succeeded || details.Data == null)
        {
            return NotFound();
        }
        Details = details.Data;
        ViewData["StatusPath"] = $"admin / users / {Details.Email}";
        return Page();
    }

    public async Task<IActionResult> OnPostMessageAsync(string id, string? subject, string? message)
    {
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            TempData["ToastError"] = "Enter a subject and a message.";
        }
        else
        {
            var sent = await _messages.SendBulkMessageAsync(new List<string> { id }, User.GetUserId()!, subject.Trim(), message.Trim());
            if (sent.Succeeded) TempData["ToastSuccess"] = "Message sent";
            else TempData["ToastError"] = sent.Error ?? "The message could not be sent.";
        }
        return RedirectToPage(new { id });
    }
}
