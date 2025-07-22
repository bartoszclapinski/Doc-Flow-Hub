using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.Admin;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Web.Extensions;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Web.Pages.Admin.Users;

[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private readonly IUserManagementService _userManagementService;
    private readonly IUserActivityService _userActivityService;
    private readonly IUserSecurityService _userSecurityService;
    private readonly IUserCommunicationService _userCommunicationService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUserManagementService userManagementService,
        IUserActivityService userActivityService,
        IUserSecurityService userSecurityService,
        IUserCommunicationService userCommunicationService,
        ILogger<IndexModel> logger)
    {
        _userManagementService = userManagementService;
        _userActivityService = userActivityService;
        _userSecurityService = userSecurityService;
        _userCommunicationService = userCommunicationService;
        _logger = logger;
    }

    // Properties for the UI
    public PagedResult<ApplicationUser> Users { get; set; } = new();
    public UserManagementStatistics Statistics { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new();
    
    [BindProperty]
    public UserManagementFilter Filter { get; set; } = new();
    
    [BindProperty]
    public BulkUserOperation BulkOperation { get; set; } = new();
    
    [BindProperty]
    public string? SendMessageSubject { get; set; }
    
    [BindProperty]
    public string? SendMessageContent { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(
        string? searchTerm = null,
        UserStatus? status = null,
        string? role = null,
        DateTime? registeredAfter = null,
        DateTime? registeredBefore = null,
        ActivityLevel? activityLevel = null,
        string? sortBy = "Name",
        bool sortDescending = false,
        int page = 1,
        int pageSize = 20)
    {
        // Set up filter from query parameters
        Filter = new UserManagementFilter
        {
            SearchTerm = searchTerm,
            Status = status,
            Role = role,
            RegisteredAfter = registeredAfter,
            RegisteredBefore = registeredBefore,
            ActivityLevel = activityLevel,
            SortBy = sortBy ?? "Name",
            SortDescending = sortDescending,
            Page = page,
            PageSize = pageSize
        };

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostBulkOperationAsync()
    {
        _logger.LogInformation("Processing bulk operation request");
        _logger.LogDebug("ModelState validation status: {IsValid}", ModelState.IsValid);
        _logger.LogDebug("Bulk operation type: {Operation}", BulkOperation?.Operation.ToString() ?? "null");
        _logger.LogDebug("Target user count: {Count}", BulkOperation?.UserIds?.Count ?? 0);

        // Custom validation for SendMessage operation
        if (BulkOperation?.Operation == BulkOperationType.SendMessage)
        {
            if (string.IsNullOrWhiteSpace(SendMessageSubject))
                ModelState.AddModelError(nameof(SendMessageSubject), "Subject is required for sending messages.");
            if (string.IsNullOrWhiteSpace(SendMessageContent))
                ModelState.AddModelError(nameof(SendMessageContent), "Message content is required for sending messages.");
        }

        // Remove validation errors for message fields if not sending messages
        if (BulkOperation?.Operation != BulkOperationType.SendMessage)
        {
            ModelState.Remove(nameof(SendMessageSubject));
            ModelState.Remove(nameof(SendMessageContent));
        }

        // Remove any potential "Page" validation error
        ModelState.Remove("Page");

        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("ModelState validation error: {Error}", error.ErrorMessage);
            }
            await LoadDataAsync();
            return Page();
        }

        try
        {
            var adminId = User.GetUserId();
            
            // Debug logging
            _logger.LogInformation("Bulk operation: {Operation}, UserIds count: {Count}, UserIds: {UserIds}", 
                BulkOperation.Operation, BulkOperation.UserIds.Count, string.Join(", ", BulkOperation.UserIds));

            if (BulkOperation.UserIds.Count == 0)
            {
                StatusMessage = "Error: No users selected for bulk operation.";
                await LoadDataAsync();
                return Page();
            }

            var result = await _userManagementService.ExecuteBulkOperationAsync(BulkOperation, adminId);

            if (result.Succeeded)
            {
                var bulkResult = result.Data;
                StatusMessage = $"Bulk operation completed: {bulkResult.SuccessfulOperations} successful, {bulkResult.FailedOperations} failed.";
                
                if (bulkResult.Errors.Count > 0)
                {
                    StatusMessage += $" Errors: {string.Join("; ", bulkResult.Errors.Take(3))}";
                    if (bulkResult.Errors.Count > 3)
                        StatusMessage += "...";
                }

                _logger.LogInformation("Bulk operation result: Success={Success}, Failed={Failed}, Errors={Errors}", 
                    bulkResult.SuccessfulOperations, bulkResult.FailedOperations, string.Join(", ", bulkResult.Errors));
            }
            else
            {
                StatusMessage = $"Error: {result.Error}";
                _logger.LogError("Bulk operation failed: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing bulk operation");
            StatusMessage = "An error occurred while executing the bulk operation.";
        }

        // Clear the bulk operation for next use
        BulkOperation = new BulkUserOperation();
        
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(SendMessageSubject) || string.IsNullOrWhiteSpace(SendMessageContent))
        {
            StatusMessage = "Error: Subject and message content are required.";
            await LoadDataAsync();
            return Page();
        }

        if (!BulkOperation.UserIds.Any())
        {
            StatusMessage = "Error: No users selected for messaging.";
            await LoadDataAsync();
            return Page();
        }

        try
        {
            var adminId = User.GetUserId();
            var result = await _userCommunicationService.SendBulkMessageAsync(
                BulkOperation.UserIds, adminId, SendMessageSubject, SendMessageContent);

            if (result.Succeeded)
            {
                StatusMessage = $"Message sent successfully to {BulkOperation.UserIds.Count} users.";
            }
            else
            {
                StatusMessage = $"Error sending message: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk message");
            StatusMessage = "An error occurred while sending the message.";
        }

        // Clear form data
        SendMessageSubject = string.Empty;
        SendMessageContent = string.Empty;
        BulkOperation = new BulkUserOperation();

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetUserDetailsAsync(string userId)
    {
        try
        {
            var userResult = await _userManagementService.GetUserByIdAsync(userId);
            if (!userResult.Succeeded)
            {
                return new JsonResult(new { success = false, error = userResult.Error });
            }

            var user = userResult.Data;
            var activityReport = await _userActivityService.GetUserActivityReportAsync(userId);
            var securityStatus = await _userSecurityService.GetUserSecurityStatusAsync(userId);

            return new JsonResult(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    createdAt = user.CreatedAt,
                    lastLoginAt = user.LastLoginAt,
                    emailConfirmed = user.EmailConfirmed,
                    twoFactorEnabled = user.TwoFactorEnabled,
                    isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
                },
                activityMetrics = activityReport.ActivityMetrics,
                usageStatistics = activityReport.UsageStatistics,
                securityStatus = securityStatus,
                recentActivities = activityReport.RecentActivities.Take(10)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user details for {UserId}", userId);
            return new JsonResult(new { success = false, error = "Error loading user details" });
        }
    }

    public async Task<IActionResult> OnPostQuickActionAsync(string userId, string action)
    {
        try
        {
            var adminId = User.GetUserId();
            ServiceResult result;

            switch (action.ToLower())
            {
                case "activate":
                    result = await _userManagementService.ActivateUsersAsync(new List<string> { userId }, adminId);
                    break;
                case "deactivate":
                    result = await _userManagementService.DeactivateUsersAsync(new List<string> { userId }, adminId);
                    break;
                case "lock":
                    result = await _userManagementService.LockUsersAsync(new List<string> { userId }, adminId);
                    break;
                case "unlock":
                    result = await _userManagementService.UnlockUsersAsync(new List<string> { userId }, adminId);
                    break;
                case "reset-password":
                    result = await _userManagementService.ResetUserPasswordAsync(userId, adminId);
                    break;
                case "confirm-email":
                    result = await _userManagementService.ConfirmUserEmailAsync(userId, adminId);
                    break;
                default:
                    return new JsonResult(new { success = false, error = "Unknown action" });
            }

            if (result.Succeeded)
            {
                return new JsonResult(new { success = true, message = $"Action '{action}' completed successfully" });
            }
            else
            {
                return new JsonResult(new { success = false, error = result.Error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing quick action {Action} for user {UserId}", action, userId);
            return new JsonResult(new { success = false, error = "Error executing action" });
        }
    }

    public async Task<IActionResult> OnGetStatisticsAsync()
    {
        try
        {
            var statistics = await _userManagementService.GetUserStatisticsAsync();
            return new JsonResult(new { success = true, data = statistics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user statistics");
            return new JsonResult(new { success = false, error = "Error loading statistics" });
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // Load users with current filter
            Users = await _userManagementService.SearchUsersAsync(Filter);
            
            // Load statistics
            Statistics = await _userManagementService.GetUserStatisticsAsync();
            
            // Load available roles
            AvailableRoles = await _userManagementService.GetAvailableRolesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user management data");
            Users = new PagedResult<ApplicationUser>();
            Statistics = new UserManagementStatistics();
            AvailableRoles = new List<string>();
        }
    }
} 