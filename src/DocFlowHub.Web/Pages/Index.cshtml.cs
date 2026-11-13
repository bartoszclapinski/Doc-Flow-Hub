using DocFlowHub.Core.Models.Dashboard;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using DocFlowHub.Web.Ui;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages;

/// <summary>
/// Dashboard (design 3a) for every signed-in user, administrators included; the system-wide view
/// lives at /Admin. Signed-out visitors go straight to Sign in (the marketing page is gone).
/// </summary>
public class IndexModel : PageModel
{
    private readonly IDashboardService _dashboardService;
    private readonly IAIUsageTrackingService _aiUsage;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IDashboardService dashboardService, IAIUsageTrackingService aiUsage, ILogger<IndexModel> logger)
    {
        _dashboardService = dashboardService;
        _aiUsage = aiUsage;
        _logger = logger;
    }

    /// <summary>"7d" (default), "30d" or "all".</summary>
    [BindProperty(SupportsGet = true)]
    public string? Period { get; set; }

    public DashboardPeriod SelectedPeriod { get; private set; }
    public UserDashboard? Dashboard { get; private set; }
    public int? AiRequestsToday { get; private set; }
    public int? AiDailyLimit { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        SelectedPeriod = DashboardPeriods.Parse(Period);

        var result = await _dashboardService.GetUserDashboardAsync(userId, SelectedPeriod);
        Dashboard = result.Succeeded ? result.Data : null;

        try
        {
            var limits = await _aiUsage.GetUserRateLimitsAsync(userId);
            if (limits.Succeeded && limits.Data != null)
            {
                AiRequestsToday = limits.Data.CurrentDailyOperations;
                AiDailyLimit = limits.Data.DailyOperationLimit;
            }
        }
        catch (Exception ex)
        {
            // The meter is optional; the rest of the dashboard still renders.
            _logger.LogWarning(ex, "Could not load AI usage for the dashboard");
        }

        return Page();
    }
}
