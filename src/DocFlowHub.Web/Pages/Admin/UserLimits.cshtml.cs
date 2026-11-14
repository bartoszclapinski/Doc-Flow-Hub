using DocFlowHub.Core.Models.Admin.Browser;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Admin;

/// <summary>
/// User limits (decision S14-6): today's AI usage of every user against the limits the app enforces.
/// The limits are the same for everyone and fixed in AIUsageTrackingService; the old "Reset usage" and
/// "Update default limits" buttons reported success without doing anything and are gone.
/// </summary>
[Authorize(Roles = "Administrator")]
public class UserLimitsModel : PageModel
{
    private readonly IAdminBrowserService _admin;
    private readonly IAIUsageTrackingService _usage;

    public UserLimitsModel(IAdminBrowserService admin, IAIUsageTrackingService usage)
    {
        _admin = admin;
        _usage = usage;
    }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Search { get; set; }

    /// <summary>"normal", "warning" (80 % or more) or "exceeded".</summary>
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public List<AiUsageToday> Rows { get; private set; } = new();
    public int DailyLimit { get; private set; }
    public int HourlyLimit { get; private set; }
    public decimal DailyCostLimit { get; private set; }
    public int Warning { get; private set; }
    public int Exceeded { get; private set; }
    public int Total { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // The limits are the same for every user: read them once from the tracking service.
        var limits = await _usage.GetUserRateLimitsAsync(User.GetUserId()!);
        var l = limits.Succeeded && limits.Data != null ? limits.Data : new UserRateLimits { DailyOperationLimit = 1, HourlyOperationLimit = 1, DailyCostLimit = 1 };
        (DailyLimit, HourlyLimit, DailyCostLimit) = (l.DailyOperationLimit, l.HourlyOperationLimit, l.DailyCostLimit);

        var usage = await _admin.GetAiUsageTodayAsync();
        var all = usage.Succeeded && usage.Data != null ? usage.Data : new List<AiUsageToday>();
        Total = all.Count;
        Warning = all.Count(u => StateOf(u) == "warning");
        Exceeded = all.Count(u => StateOf(u) == "exceeded");

        var q = Search?.Trim();
        Rows = all
            .Where(u => string.IsNullOrEmpty(q) || u.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || u.Email.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Where(u => Status is not ("normal" or "warning" or "exceeded") || StateOf(u) == Status)
            .ToList();
        return Page();
    }

    /// <summary>Same rule as UserRateLimits: over at the daily request or cost limit, warning from 80 %.</summary>
    public string StateOf(AiUsageToday u) =>
        u.Requests >= DailyLimit || u.Cost >= DailyCostLimit ? "exceeded"
        : u.Requests >= DailyLimit * 0.8 ? "warning"
        : "normal";
}
