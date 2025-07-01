using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Common;

namespace DocFlowHub.Web.Pages.Admin;

[Authorize(Roles = "Administrator")]
public class AnalyticsModel : PageModel
{
    private readonly IAIUsageTrackingService _usageTrackingService;
    private readonly ILogger<AnalyticsModel> _logger;

    public AnalyticsModel(
        IAIUsageTrackingService usageTrackingService,
        ILogger<AnalyticsModel> logger)
    {
        _usageTrackingService = usageTrackingService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int Period { get; set; } = 30;

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

        // Load system usage statistics
        var systemStatsResult = await _usageTrackingService.GetSystemUsageStatisticsAsync(fromDate);
        if (systemStatsResult.Succeeded)
        {
            SystemStats = systemStatsResult.Data;
            _logger.LogDebug("Loaded system statistics for {Period} days", Period);
        }
        else
        {
            _logger.LogWarning("Failed to load system statistics: {Error}", systemStatsResult.Error);
            SystemStats = new SystemUsageStatistics(); // Provide empty stats to prevent UI errors
        }

        // Load usage trends
        var trendsResult = await _usageTrackingService.GetUsageTrendsAsync(null, Period);
        if (trendsResult.Succeeded)
        {
            UsageTrends = trendsResult.Data;
            _logger.LogDebug("Loaded usage trends for {Period} days", Period);
        }
        else
        {
            _logger.LogWarning("Failed to load usage trends: {Error}", trendsResult.Error);
            UsageTrends = new UsageTrends(); // Provide empty trends to prevent UI errors
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

        // Load error statistics
        var errorStatsResult = await _usageTrackingService.GetErrorRateStatisticsAsync(fromDate);
        if (errorStatsResult.Succeeded)
        {
            ErrorStats = errorStatsResult.Data;
            _logger.LogDebug("Loaded error statistics with {ErrorRate:F2}% error rate", 
                ErrorStats?.OverallErrorRate ?? 0);
        }
        else
        {
            _logger.LogWarning("Failed to load error statistics: {Error}", errorStatsResult.Error);
            ErrorStats = new ErrorRateStatistics(); // Provide empty stats to prevent UI errors
        }

        _logger.LogInformation("Analytics data loaded successfully for {Period} day period", Period);
    }
} 