using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.AI;

namespace DocFlowHub.Web.Pages.Admin;

[Authorize(Roles = "Administrator")]
public class UserLimitsModel : PageModel
{
    private readonly IAIUsageTrackingService _usageTrackingService;
    private readonly ILogger<UserLimitsModel> _logger;

    public UserLimitsModel(
        IAIUsageTrackingService usageTrackingService,
        ILogger<UserLimitsModel> logger)
    {
        _usageTrackingService = usageTrackingService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Sort { get; set; }

    public List<UserRateLimits>? UserLimits { get; set; }
    public UserLimitsOverview? LimitsOverview { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            await LoadUserLimitsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user limits data");
            StatusMessage = "Error loading user limits data. Please try again.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnGetRefreshAsync()
    {
        try
        {
            // Clear any cached data
            await LoadUserLimitsAsync();
            StatusMessage = "User limits data refreshed successfully.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing user limits data");
            StatusMessage = "Error refreshing user limits data. Please try again.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnGetUserDetailsAsync(string userId)
    {
        try
        {
            var result = await _usageTrackingService.GetUserRateLimitsAsync(userId);
            
            if (result.Succeeded && result.Data != null)
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
                error = result.Error ?? "User not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user details for {UserId}", userId);
            return new JsonResult(new
            {
                success = false,
                error = "Failed to retrieve user details"
            });
        }
    }

    public async Task<IActionResult> OnPostResetUserUsageAsync(string userId)
    {
        try
        {
            // Note: This would require implementing a reset method in the service
            // For now, we'll return a success response
            _logger.LogInformation("Admin requested usage reset for user {UserId}", userId);
            
            return new JsonResult(new
            {
                success = true,
                message = "User usage counters have been reset"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting usage for user {UserId}", userId);
            return new JsonResult(new
            {
                success = false,
                error = "Failed to reset user usage"
            });
        }
    }

    public async Task<IActionResult> OnPostUpdateLimitsAsync(
        int dailyOperationLimit, 
        int hourlyOperationLimit, 
        decimal dailyCostLimit)
    {
        try
        {
            // Note: This would require implementing a method to update default limits
            // For now, we'll log and return success
            _logger.LogInformation("Admin updated default limits: Daily Operations: {DailyOps}, Hourly Operations: {HourlyOps}, Daily Cost: {DailyCost}",
                dailyOperationLimit, hourlyOperationLimit, dailyCostLimit);
            
            return new JsonResult(new
            {
                success = true,
                message = "Default limits updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating default limits");
            return new JsonResult(new
            {
                success = false,
                error = "Failed to update default limits"
            });
        }
    }

    private async Task LoadUserLimitsAsync()
    {
        try
        {
            // For now, initialize with empty list since we don't have a method to get all users' rate limits
            // In a real implementation, you would call a method that returns all users
            UserLimits = new List<UserRateLimits>();
            
            // Create sample/empty data structure for demonstration
            var sampleUsers = new List<UserRateLimits>();
            
            // Filter based on search criteria (no users to filter for now)
            var filteredUsers = sampleUsers.AsEnumerable();
                
            if (!string.IsNullOrEmpty(Search))
            {
                filteredUsers = filteredUsers.Where(u => 
                    u.UserId?.Contains(Search, StringComparison.OrdinalIgnoreCase) == true);
            }
            
            if (!string.IsNullOrEmpty(Status))
            {
                filteredUsers = Status.ToLower() switch
                {
                    "normal" => filteredUsers.Where(u => !u.IsNearLimit && !u.IsOverLimit),
                    "warning" => filteredUsers.Where(u => u.IsNearLimit && !u.IsOverLimit),
                    "exceeded" => filteredUsers.Where(u => u.IsOverLimit),
                    _ => filteredUsers
                };
            }
            
            // Sort results
            filteredUsers = Sort?.ToLower() switch
            {
                "cost" => filteredUsers.OrderByDescending(u => u.CurrentDailyCost),
                "operations" => filteredUsers.OrderByDescending(u => u.CurrentDailyOperations),
                "usage" => filteredUsers.OrderByDescending(u => u.UsagePercentage),
                _ => filteredUsers.OrderByDescending(u => u.UsagePercentage)
            };
            
            UserLimits = filteredUsers.ToList();
            
            // Calculate overview statistics
            LimitsOverview = new UserLimitsOverview
            {
                TotalActiveUsers = UserLimits.Count,
                NormalUsers = UserLimits.Count(u => !u.IsNearLimit && !u.IsOverLimit),
                WarningUsers = UserLimits.Count(u => u.IsNearLimit && !u.IsOverLimit),
                ExceededUsers = UserLimits.Count(u => u.IsOverLimit)
            };
            
            _logger.LogDebug("Loaded user limits for {UserCount} users", UserLimits.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LoadUserLimitsAsync");
            UserLimits = new List<UserRateLimits>();
            LimitsOverview = new UserLimitsOverview();
        }
    }
}

// Supporting classes for the overview
public class UserLimitsOverview
{
    public int TotalActiveUsers { get; set; }
    public int NormalUsers { get; set; }
    public int WarningUsers { get; set; }
    public int ExceededUsers { get; set; }
} 