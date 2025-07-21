using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Analytics;
using DocFlowHub.Core.Models.Common;

namespace DocFlowHub.Web.Pages.Admin;

[Authorize(Roles = "Administrator")]
public class AnalyticsModel : PageModel
{
    private readonly IAIUsageTrackingService _usageTrackingService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsModel> _logger;

    public AnalyticsModel(
        IAIUsageTrackingService usageTrackingService,
        IAnalyticsService analyticsService,
        ILogger<AnalyticsModel> logger)
    {
        _usageTrackingService = usageTrackingService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int Period { get; set; } = 30;

    // New comprehensive analytics properties
    public SystemAnalytics? SystemAnalytics { get; set; }
    public AnalyticsTrends? AnalyticsTrends { get; set; }
    public RealTimeDashboard? RealTimeDashboard { get; set; }
    
    // Legacy AI-specific properties (for backward compatibility)
    public SystemUsageStatistics? SystemStats { get; set; }
    public UsageTrends? UsageTrends { get; set; }
    public List<ExpensiveOperation>? ExpensiveOperations { get; set; }
    public ErrorRateStatistics? ErrorStats { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["ActivePage"] = "Analytics";
        
        try
        {
            await LoadAnalyticsDataAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin analytics data");
            StatusMessage = "Error loading analytics data. Please try again.";
            return Page();
        }
    }

    public async Task<IActionResult> OnGetRefreshAsync()
    {
        try
        {
            await LoadAnalyticsDataAsync();
            StatusMessage = "Analytics data refreshed successfully.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing analytics data");
            StatusMessage = "Error refreshing analytics data. Please try again.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnGetSystemAnalyticsAsync()
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-Period);
            var result = await _analyticsService.GetSystemAnalyticsAsync(fromDate);
            
            if (result.Succeeded)
            {
                return new JsonResult(new
                {
                    success = true,
                    data = result.Data
                });
            }

            return new JsonResult(new
            {
                success = false,
                error = result.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system analytics via API");
            return new JsonResult(new
            {
                success = false,
                error = "Failed to retrieve system analytics"
            });
        }
    }

    public async Task<IActionResult> OnGetRealTimeDashboardAsync()
    {
        try
        {
            var result = await _analyticsService.GetRealTimeDashboardAsync();
            
            if (result.Succeeded)
            {
                return new JsonResult(new
                {
                    success = true,
                    data = result.Data
                });
            }

            return new JsonResult(new
            {
                success = false,
                error = result.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real-time dashboard via API");
            return new JsonResult(new
            {
                success = false,
                error = "Failed to retrieve real-time dashboard"
            });
        }
    }

    public async Task<IActionResult> OnGetUsageStatisticsAsync()
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-Period);
            var result = await _usageTrackingService.GetSystemUsageStatisticsAsync(fromDate);
            
            if (result.Succeeded)
            {
                return new JsonResult(new
                {
                    success = true,
                    data = result.Data
                });
            }

            return new JsonResult(new
            {
                success = false,
                error = result.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage statistics via API");
            return new JsonResult(new
            {
                success = false,
                error = "Failed to retrieve usage statistics"
            });
        }
    }

    public async Task<IActionResult> OnGetUsageTrendsAsync()
    {
        try
        {
            var result = await _usageTrackingService.GetUsageTrendsAsync(null, Period);
            
            if (result.Succeeded)
            {
                return new JsonResult(new
                {
                    success = true,
                    data = result.Data
                });
            }

            return new JsonResult(new
            {
                success = false,
                error = result.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage trends via API");
            return new JsonResult(new
            {
                success = false,
                error = "Failed to retrieve usage trends"
            });
        }
    }

    public async Task<IActionResult> OnGetExpensiveOperationsAsync()
    {
        try
        {
            var result = await _usageTrackingService.GetMostExpensiveOperationsAsync(null, 10);
            
            if (result.Succeeded)
            {
                return new JsonResult(new
                {
                    success = true,
                    data = result.Data
                });
            }

            return new JsonResult(new
            {
                success = false,
                error = result.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expensive operations via API");
            return new JsonResult(new
            {
                success = false,
                error = "Failed to retrieve expensive operations"
            });
        }
    }

    public async Task<IActionResult> OnGetErrorStatisticsAsync()
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-Period);
            var result = await _usageTrackingService.GetErrorRateStatisticsAsync(fromDate);
            
            if (result.Succeeded)
            {
                return new JsonResult(new
                {
                    success = true,
                    data = result.Data
                });
            }

            return new JsonResult(new
            {
                success = false,
                error = result.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error statistics via API");
            return new JsonResult(new
            {
                success = false,
                error = "Failed to retrieve error statistics"
            });
        }
    }

    private async Task LoadAnalyticsDataAsync()
    {
        var fromDate = DateTime.UtcNow.AddDays(-Period);
        var toDate = DateTime.UtcNow;

        // Load comprehensive system analytics
        var systemAnalyticsResult = await _analyticsService.GetSystemAnalyticsAsync(fromDate, toDate);
        if (systemAnalyticsResult.Succeeded)
        {
            SystemAnalytics = systemAnalyticsResult.Data;
            _logger.LogDebug("Loaded comprehensive system analytics for {Period} days", Period);
        }
        else
        {
            _logger.LogWarning("Failed to load system analytics: {Error}", systemAnalyticsResult.Error);
            SystemAnalytics = new SystemAnalytics(); // Provide empty analytics to prevent UI errors
        }

        // Load analytics trends
        var trendsResult = await _analyticsService.GetAnalyticsTrendsAsync(fromDate, toDate);
        if (trendsResult.Succeeded)
        {
            AnalyticsTrends = trendsResult.Data;
            _logger.LogDebug("Loaded analytics trends for {Period} days", Period);
        }
        else
        {
            _logger.LogWarning("Failed to load analytics trends: {Error}", trendsResult.Error);
            AnalyticsTrends = new AnalyticsTrends { StartDate = fromDate, EndDate = toDate };
        }

        // Load real-time dashboard
        var dashboardResult = await _analyticsService.GetRealTimeDashboardAsync();
        if (dashboardResult.Succeeded)
        {
            RealTimeDashboard = dashboardResult.Data;
            _logger.LogDebug("Loaded real-time dashboard data");
        }
        else
        {
            _logger.LogWarning("Failed to load real-time dashboard: {Error}", dashboardResult.Error);
            RealTimeDashboard = new RealTimeDashboard { LastUpdated = DateTime.UtcNow };
        }

        // Legacy AI analytics (for backward compatibility)
        var systemStatsResult = await _usageTrackingService.GetSystemUsageStatisticsAsync(fromDate);
        if (systemStatsResult.Succeeded)
        {
            SystemStats = systemStatsResult.Data;
            _logger.LogDebug("Loaded legacy AI system statistics for {Period} days", Period);
        }
        else
        {
            _logger.LogWarning("Failed to load legacy system statistics: {Error}", systemStatsResult.Error);
            SystemStats = new SystemUsageStatistics(); // Provide empty stats to prevent UI errors
        }

        // Load expensive operations
        var expensiveOpsResult = await _usageTrackingService.GetMostExpensiveOperationsAsync(null, 10);
        if (expensiveOpsResult.Succeeded)
        {
            ExpensiveOperations = expensiveOpsResult.Data;
            _logger.LogDebug("Loaded {Count} expensive operations", ExpensiveOperations?.Count ?? 0);
        }
        else
        {
            _logger.LogWarning("Failed to load expensive operations: {Error}", expensiveOpsResult.Error);
            ExpensiveOperations = new List<ExpensiveOperation>(); // Provide empty list to prevent UI errors
        }

        _logger.LogInformation("Comprehensive analytics data loaded successfully for {Period} day period", Period);
    }
} 