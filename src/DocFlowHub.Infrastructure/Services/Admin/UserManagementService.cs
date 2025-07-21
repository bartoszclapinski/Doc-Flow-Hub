using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Admin;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DocFlowHub.Infrastructure.Services.Admin;

public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserManagementService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<PagedResult<ApplicationUser>> SearchUsersAsync(UserManagementFilter filter)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            // Apply search term filter
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(u => 
                    u.UserName!.ToLower().Contains(searchTerm) ||
                    u.Email!.ToLower().Contains(searchTerm) ||
                    u.FirstName!.ToLower().Contains(searchTerm) ||
                    u.LastName!.ToLower().Contains(searchTerm));
            }

            // Apply status filter
            if (filter.Status.HasValue && filter.Status != UserStatus.All)
            {
                switch (filter.Status.Value)
                {
                    case UserStatus.Active:
                        query = query.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow);
                        break;
                    case UserStatus.Locked:
                        query = query.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow);
                        break;
                    case UserStatus.EmailNotConfirmed:
                        query = query.Where(u => !u.EmailConfirmed);
                        break;
                    case UserStatus.Inactive:
                        // Users with no recent activity (placeholder logic)
                        var inactiveThreshold = DateTime.UtcNow.AddDays(-30);
                        query = query.Where(u => !u.LastLoginAt.HasValue || u.LastLoginAt < inactiveThreshold);
                        break;
                }
            }

            // Apply date range filters
            if (filter.RegisteredAfter.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= filter.RegisteredAfter.Value);
            }

            if (filter.RegisteredBefore.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= filter.RegisteredBefore.Value);
            }

            if (filter.LastLoginAfter.HasValue)
            {
                query = query.Where(u => u.LastLoginAt >= filter.LastLoginAfter.Value);
            }

            if (filter.LastLoginBefore.HasValue)
            {
                query = query.Where(u => u.LastLoginAt <= filter.LastLoginBefore.Value);
            }

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "email" => filter.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "createdat" => filter.SortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                "lastloginat" => filter.SortDescending ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
                _ => filter.SortDescending ? query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName) : query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            };

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<ApplicationUser>
            {
                Items = users,
                TotalItems = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with filter: {@Filter}", filter);
            return new PagedResult<ApplicationUser>();
        }
    }

    public async Task<UserManagementStatistics> GetUserStatisticsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekAgo = today.AddDays(-7);
            var monthAgo = today.AddMonths(-1);

            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow);
            var lockedUsers = await _context.Users.CountAsync(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow);
            var unconfirmedUsers = await _context.Users.CountAsync(u => !u.EmailConfirmed);
            var newUsersToday = await _context.Users.CountAsync(u => u.CreatedAt >= today);
            var newUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= weekAgo);
            var newUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= monthAgo);

            var roleDistribution = new List<RoleDistribution>();
            var roles = await _roleManager.Roles.ToListAsync();
            
            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                roleDistribution.Add(new RoleDistribution
                {
                    RoleName = role.Name!,
                    UserCount = usersInRole.Count,
                    Percentage = totalUsers > 0 ? (decimal)usersInRole.Count / totalUsers * 100 : 0
                });
            }

            return new UserManagementStatistics
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = totalUsers - activeUsers,
                LockedUsers = lockedUsers,
                UnconfirmedUsers = unconfirmedUsers,
                NewUsersToday = newUsersToday,
                NewUsersThisWeek = newUsersThisWeek,
                NewUsersThisMonth = newUsersThisMonth,
                RoleDistribution = roleDistribution,
                ActivityDistribution = new List<ActivityLevelDistribution>() // Placeholder
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user statistics");
            return new UserManagementStatistics();
        }
    }

    public async Task<List<string>> GetAvailableRolesAsync()
    {
        try
        {
            return await _roleManager.Roles
                .Select(r => r.Name!)
                .OrderBy(name => name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available roles");
            return new List<string>();
        }
    }

    public async Task<ServiceResult<BulkOperationResult>> ExecuteBulkOperationAsync(BulkUserOperation operation, string adminId)
    {
        try
        {
            var result = new BulkOperationResult
            {
                TotalUsers = operation.UserIds.Count
            };

            foreach (var userId in operation.UserIds)
            {
                try
                {
                    var operationResult = operation.Operation switch
                    {
                        BulkOperationType.Activate => await ActivateUserAsync(userId, adminId),
                        BulkOperationType.Deactivate => await DeactivateUserAsync(userId, adminId),
                        BulkOperationType.Delete => await DeleteUserAsync(userId, adminId),
                        BulkOperationType.AssignRole => await AssignRoleToUserAsync(userId, operation.NewRole!, adminId),
                        BulkOperationType.RemoveRole => await RemoveRoleFromUserAsync(userId, operation.NewRole!, adminId),
                        BulkOperationType.LockAccount => await LockUserAsync(userId, adminId),
                        BulkOperationType.UnlockAccount => await UnlockUserAsync(userId, adminId),
                        BulkOperationType.SendPasswordReset => await SendPasswordResetToUserAsync(userId, adminId),
                        _ => ServiceResult.Failure($"Unknown operation: {operation.Operation}")
                    };

                    if (operationResult.Succeeded)
                    {
                        result.SuccessfulOperations++;
                        result.SuccessfulUserIds.Add(userId);
                    }
                    else
                    {
                        result.FailedOperations++;
                        result.FailedUserIds.Add(userId);
                        result.Errors.Add($"User {userId}: {operationResult.Error}");
                    }
                }
                catch (Exception ex)
                {
                    result.FailedOperations++;
                    result.FailedUserIds.Add(userId);
                    result.Errors.Add($"User {userId}: {ex.Message}");
                    _logger.LogError(ex, "Error processing bulk operation for user {UserId}", userId);
                }
            }

            await LogUserActivityAsync(adminId, "BulkOperation", 
                $"Executed {operation.Operation} on {operation.UserIds.Count} users. Success: {result.SuccessfulOperations}, Failed: {result.FailedOperations}");

            return ServiceResult<BulkOperationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing bulk operation {@Operation}", operation);
            return ServiceResult<BulkOperationResult>.Failure("Failed to execute bulk operation");
        }
    }

    // Individual user operations
    private async Task<ServiceResult> ActivateUserAsync(string userId, string adminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ServiceResult.Failure("User not found");

        if (user.LockoutEnd.HasValue)
        {
            user.LockoutEnd = null;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await LogUserActivityAsync(adminId, "UserActivated", $"Activated user {user.Email}", userId, user.Email);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult> DeactivateUserAsync(string userId, string adminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ServiceResult.Failure("User not found");

        user.LockoutEnd = DateTimeOffset.MaxValue;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        await LogUserActivityAsync(adminId, "UserDeactivated", $"Deactivated user {user.Email}", userId, user.Email);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult> DeleteUserAsync(string userId, string adminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ServiceResult.Failure("User not found");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        await LogUserActivityAsync(adminId, "UserDeleted", $"Deleted user {user.Email}", userId, user.Email);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult> AssignRoleToUserAsync(string userId, string role, string adminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ServiceResult.Failure("User not found");

        if (!await _roleManager.RoleExistsAsync(role))
            return ServiceResult.Failure($"Role '{role}' does not exist");

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        await LogUserActivityAsync(adminId, "RoleAssigned", $"Assigned role '{role}' to user {user.Email}", userId, user.Email);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult> RemoveRoleFromUserAsync(string userId, string role, string adminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ServiceResult.Failure("User not found");

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
            return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        await LogUserActivityAsync(adminId, "RoleRemoved", $"Removed role '{role}' from user {user.Email}", userId, user.Email);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult> LockUserAsync(string userId, string adminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ServiceResult.Failure("User not found");

        user.LockoutEnd = DateTimeOffset.MaxValue;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        await LogUserActivityAsync(adminId, "UserLocked", $"Locked user {user.Email}", userId, user.Email);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult> UnlockUserAsync(string userId, string adminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ServiceResult.Failure("User not found");

        user.LockoutEnd = null;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        await LogUserActivityAsync(adminId, "UserUnlocked", $"Unlocked user {user.Email}", userId, user.Email);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult> SendPasswordResetToUserAsync(string userId, string adminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ServiceResult.Failure("User not found");

        // Generate password reset token (in real implementation, send email)
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        await LogUserActivityAsync(adminId, "PasswordResetSent", $"Sent password reset to user {user.Email}", userId, user.Email);
        return ServiceResult.Success();
    }

    // Helper method for logging user activities
    private async Task LogUserActivityAsync(string userId, string activityType, string description, string? resourceId = null, string? resourceName = null)
    {
        try
        {
            var activity = new UserActivityLog
            {
                UserId = userId,
                ActivityType = activityType,
                Description = description,
                ResourceId = resourceId,
                ResourceName = resourceName,
                IpAddress = "127.0.0.1", // In real implementation, get from HttpContext
                UserAgent = "Admin System",
                Timestamp = DateTime.UtcNow
            };

            _context.UserActivityLogs.Add(activity);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging user activity");
        }
    }

    // Public interface implementations for bulk operations
    public async Task<ServiceResult> ActivateUsersAsync(List<string> userIds, string adminId) =>
        (await ExecuteBulkOperationAsync(new BulkUserOperation { UserIds = userIds, Operation = BulkOperationType.Activate }, adminId)).ToServiceResult();

    public async Task<ServiceResult> DeactivateUsersAsync(List<string> userIds, string adminId) =>
        (await ExecuteBulkOperationAsync(new BulkUserOperation { UserIds = userIds, Operation = BulkOperationType.Deactivate }, adminId)).ToServiceResult();

    public async Task<ServiceResult> DeleteUsersAsync(List<string> userIds, string adminId) =>
        (await ExecuteBulkOperationAsync(new BulkUserOperation { UserIds = userIds, Operation = BulkOperationType.Delete }, adminId)).ToServiceResult();

    public async Task<ServiceResult> AssignRoleToUsersAsync(List<string> userIds, string role, string adminId) =>
        (await ExecuteBulkOperationAsync(new BulkUserOperation { UserIds = userIds, Operation = BulkOperationType.AssignRole, NewRole = role }, adminId)).ToServiceResult();

    public async Task<ServiceResult> RemoveRoleFromUsersAsync(List<string> userIds, string role, string adminId) =>
        (await ExecuteBulkOperationAsync(new BulkUserOperation { UserIds = userIds, Operation = BulkOperationType.RemoveRole, NewRole = role }, adminId)).ToServiceResult();

    public async Task<ServiceResult> LockUsersAsync(List<string> userIds, string adminId) =>
        (await ExecuteBulkOperationAsync(new BulkUserOperation { UserIds = userIds, Operation = BulkOperationType.LockAccount }, adminId)).ToServiceResult();

    public async Task<ServiceResult> UnlockUsersAsync(List<string> userIds, string adminId) =>
        (await ExecuteBulkOperationAsync(new BulkUserOperation { UserIds = userIds, Operation = BulkOperationType.UnlockAccount }, adminId)).ToServiceResult();

    public async Task<ServiceResult> SendPasswordResetToUsersAsync(List<string> userIds, string adminId) =>
        (await ExecuteBulkOperationAsync(new BulkUserOperation { UserIds = userIds, Operation = BulkOperationType.SendPasswordReset }, adminId)).ToServiceResult();

    // Individual user management methods
    public async Task<ServiceResult<ApplicationUser>> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null 
                ? ServiceResult<ApplicationUser>.Success(user)
                : ServiceResult<ApplicationUser>.Failure("User not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
            return ServiceResult<ApplicationUser>.Failure("Error retrieving user");
        }
    }

    public async Task<ServiceResult> UpdateUserAsync(string userId, ApplicationUser updatedUser, string adminId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.Failure("User not found");

            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.Email = updatedUser.Email;
            user.UserName = updatedUser.UserName;
            user.PhoneNumber = updatedUser.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

            await LogUserActivityAsync(adminId, "UserUpdated", $"Updated user {user.Email}", userId, user.Email);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return ServiceResult.Failure("Error updating user");
        }
    }

    public async Task<ServiceResult> ResetUserPasswordAsync(string userId, string adminId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.Failure("User not found");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // In real implementation, send email with token

            await LogUserActivityAsync(adminId, "PasswordResetInitiated", $"Initiated password reset for user {user.Email}", userId, user.Email);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            return ServiceResult.Failure("Error resetting password");
        }
    }

    public async Task<ServiceResult> ConfirmUserEmailAsync(string userId, string adminId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.Failure("User not found");

            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

            await LogUserActivityAsync(adminId, "EmailConfirmed", $"Confirmed email for user {user.Email}", userId, user.Email);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email for user {UserId}", userId);
            return ServiceResult.Failure("Error confirming email");
        }
    }

    public async Task<ServiceResult> SetUserTwoFactorAsync(string userId, bool enabled, string adminId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.Failure("User not found");

            var result = await _userManager.SetTwoFactorEnabledAsync(user, enabled);
            if (!result.Succeeded)
                return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

            await LogUserActivityAsync(adminId, "TwoFactorChanged", 
                $"{(enabled ? "Enabled" : "Disabled")} two-factor authentication for user {user.Email}", userId, user.Email);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting two-factor for user {UserId}", userId);
            return ServiceResult.Failure("Error updating two-factor authentication");
        }
    }

    // Security-related methods (placeholder implementations)
    public async Task<ServiceResult> RecordLoginAttemptAsync(string userId, string ipAddress, string userAgent, bool isSuccessful, string? failureReason = null)
    {
        try
        {
            var loginAttempt = new UserLoginAttempt
            {
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = isSuccessful,
                FailureReason = failureReason,
                AttemptedAt = DateTime.UtcNow
            };

            _context.UserLoginAttempts.Add(loginAttempt);
            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording login attempt for user {UserId}", userId);
            return ServiceResult.Failure("Error recording login attempt");
        }
    }

    public async Task<ServiceResult> RegisterDeviceAsync(string userId, string deviceFingerprint, string deviceInfo, string ipAddress)
    {
        try
        {
            var existingDevice = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceFingerprint == deviceFingerprint);

            if (existingDevice != null)
            {
                existingDevice.LastSeen = DateTime.UtcNow;
                existingDevice.LastIpAddress = ipAddress;
            }
            else
            {
                var device = new UserDevice
                {
                    UserId = userId,
                    DeviceFingerprint = deviceFingerprint,
                    DeviceType = "Unknown", // Parse from deviceInfo in real implementation
                    LastIpAddress = ipAddress,
                    FirstSeen = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };

                _context.UserDevices.Add(device);
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device for user {UserId}", userId);
            return ServiceResult.Failure("Error registering device");
        }
    }

    public async Task<ServiceResult> CreateSecurityEventAsync(string userId, string eventType, string description, string severity, string ipAddress, string? eventData = null)
    {
        try
        {
            var securityEvent = new UserSecurityEvent
            {
                UserId = userId,
                EventType = eventType,
                Description = description,
                Severity = severity,
                IpAddress = ipAddress,
                EventData = eventData,
                Timestamp = DateTime.UtcNow
            };

            _context.UserSecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating security event for user {UserId}", userId);
            return ServiceResult.Failure("Error creating security event");
        }
    }

    public async Task<List<UserLoginAttempt>> GetRecentLoginAttemptsAsync(string userId, int count = 10)
    {
        try
        {
            return await _context.UserLoginAttempts
                .Where(la => la.UserId == userId)
                .OrderByDescending(la => la.AttemptedAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting login attempts for user {UserId}", userId);
            return new List<UserLoginAttempt>();
        }
    }

    public async Task<List<UserDevice>> GetUserDevicesAsync(string userId)
    {
        try
        {
            return await _context.UserDevices
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.LastSeen)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices for user {UserId}", userId);
            return new List<UserDevice>();
        }
    }

    public async Task<ServiceResult> TrustDeviceAsync(int deviceId, string adminId)
    {
        try
        {
            var device = await _context.UserDevices.FindAsync(deviceId);
            if (device == null) return ServiceResult.Failure("Device not found");

            device.IsTrusted = true;
            await _context.SaveChangesAsync();

            await LogUserActivityAsync(adminId, "DeviceTrusted", $"Trusted device {device.DeviceFingerprint} for user", device.UserId);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trusting device {DeviceId}", deviceId);
            return ServiceResult.Failure("Error trusting device");
        }
    }

    public async Task<ServiceResult> RemoveDeviceAsync(int deviceId, string adminId)
    {
        try
        {
            var device = await _context.UserDevices.FindAsync(deviceId);
            if (device == null) return ServiceResult.Failure("Device not found");

            _context.UserDevices.Remove(device);
            await _context.SaveChangesAsync();

            await LogUserActivityAsync(adminId, "DeviceRemoved", $"Removed device {device.DeviceFingerprint} for user", device.UserId);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing device {DeviceId}", deviceId);
            return ServiceResult.Failure("Error removing device");
        }
    }
}

// Extension method to convert ServiceResult<T> to ServiceResult
public static class ServiceResultExtensions
{
    public static ServiceResult ToServiceResult<T>(this ServiceResult<T> result)
    {
        return result.Succeeded ? ServiceResult.Success() : ServiceResult.Failure(result.Error);
    }
} 