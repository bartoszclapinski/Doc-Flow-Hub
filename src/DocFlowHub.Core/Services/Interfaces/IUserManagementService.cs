using DocFlowHub.Core.Models.Admin;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Identity;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IUserManagementService
{
    // Advanced User Search & Filtering
    Task<PagedResult<ApplicationUser>> SearchUsersAsync(UserManagementFilter filter);
    Task<UserManagementStatistics> GetUserStatisticsAsync();
    Task<List<string>> GetAvailableRolesAsync();
    
    // Bulk Operations
    Task<ServiceResult<BulkOperationResult>> ExecuteBulkOperationAsync(BulkUserOperation operation, string adminId);
    Task<ServiceResult> ActivateUsersAsync(List<string> userIds, string adminId);
    Task<ServiceResult> DeactivateUsersAsync(List<string> userIds, string adminId);
    Task<ServiceResult> DeleteUsersAsync(List<string> userIds, string adminId);
    Task<ServiceResult> AssignRoleToUsersAsync(List<string> userIds, string role, string adminId);
    Task<ServiceResult> RemoveRoleFromUsersAsync(List<string> userIds, string role, string adminId);
    Task<ServiceResult> LockUsersAsync(List<string> userIds, string adminId);
    Task<ServiceResult> UnlockUsersAsync(List<string> userIds, string adminId);
    Task<ServiceResult> SendPasswordResetToUsersAsync(List<string> userIds, string adminId);
    
    // Individual User Management
    Task<ServiceResult<ApplicationUser>> GetUserByIdAsync(string userId);
    Task<ServiceResult> UpdateUserAsync(string userId, ApplicationUser updatedUser, string adminId);
    Task<ServiceResult> ResetUserPasswordAsync(string userId, string adminId);
    Task<ServiceResult> ConfirmUserEmailAsync(string userId, string adminId);
    Task<ServiceResult> SetUserTwoFactorAsync(string userId, bool enabled, string adminId);
    
    // Security Management
    Task<ServiceResult> RecordLoginAttemptAsync(string userId, string ipAddress, string userAgent, bool isSuccessful, string? failureReason = null);
    Task<ServiceResult> RegisterDeviceAsync(string userId, string deviceFingerprint, string deviceInfo, string ipAddress);
    Task<ServiceResult> CreateSecurityEventAsync(string userId, string eventType, string description, string severity, string ipAddress, string? eventData = null);
    Task<List<UserLoginAttempt>> GetRecentLoginAttemptsAsync(string userId, int count = 10);
    Task<List<UserDevice>> GetUserDevicesAsync(string userId);
    Task<ServiceResult> TrustDeviceAsync(int deviceId, string adminId);
    Task<ServiceResult> RemoveDeviceAsync(int deviceId, string adminId);
}

public interface IUserActivityService
{
    // Activity Tracking
    Task<ServiceResult> LogUserActivityAsync(string userId, string activityType, string description, string? resourceId = null, string? resourceName = null, string ipAddress = "", string userAgent = "", string? additionalData = null);
    Task<UserActivityReport> GetUserActivityReportAsync(string userId);
    Task<List<UserActivityEntry>> GetUserActivitiesAsync(string userId, int count = 50);
    Task<PagedResult<UserActivityLog>> GetSystemActivitiesAsync(int page = 1, int pageSize = 50, string? userId = null, string? activityType = null, DateTime? fromDate = null, DateTime? toDate = null);
    
    // Activity Analytics
    Task<UserActivityMetrics> CalculateUserActivityMetricsAsync(string userId);
    Task<ActivityLevel> DetermineUserActivityLevelAsync(string userId);
    Task<List<UserActivityReport>> GetMostActiveUsersAsync(int count = 10);
    Task<List<UserActivityReport>> GetLeastActiveUsersAsync(int count = 10);
    
    // Usage Statistics
    Task<UserUsageStatistics> CalculateUserUsageStatisticsAsync(string userId);
    Task<long> GetUserStorageUsageAsync(string userId);
    Task<int> GetUserAITokenUsageAsync(string userId);
}

public interface IUserSecurityService
{
    // Security Status
    Task<UserSecurityStatus> GetUserSecurityStatusAsync(string userId);
    Task<int> CalculateSecurityScoreAsync(string userId);
    Task<List<SecurityAlert>> GetUserSecurityAlertsAsync(string userId, bool includeResolved = false);
    
    // Security Monitoring
    Task<ServiceResult> CheckForSuspiciousActivityAsync(string userId);
    Task<ServiceResult> DetectUnusualLoginPatternsAsync(string userId, string ipAddress, string userAgent);
    Task<List<UserSecurityEvent>> GetSecurityEventsAsync(string? userId = null, int count = 100, string? severity = null);
    Task<ServiceResult> ResolveSecurityEventAsync(int eventId, string adminId);
    
    // Password & Authentication Security
    Task<ServiceResult> ValidatePasswordStrengthAsync(string password);
    Task<ServiceResult> CheckPasswordHistoryAsync(string userId, string newPassword);
    Task<bool> IsPasswordExpiredAsync(string userId);
    Task<ServiceResult> ForcePasswordChangeAsync(string userId, string adminId);
}

public interface IUserCommunicationService
{
    // Direct Communication
    Task<ServiceResult> SendMessageToUserAsync(string userId, string adminId, string subject, string message, CommunicationType type = CommunicationType.AdminMessage);
    Task<ServiceResult> SendBulkMessageAsync(List<string> userIds, string adminId, string subject, string message);
    Task<PagedResult<UserCommunicationEntity>> GetUserCommunicationsAsync(string userId, int page = 1, int pageSize = 20);
    Task<PagedResult<UserCommunicationEntity>> GetAdminCommunicationsAsync(string adminId, int page = 1, int pageSize = 50);
    
    // System Notifications
    Task<ServiceResult> SendWelcomeMessageAsync(string userId);
    Task<ServiceResult> SendAccountUpdateNotificationAsync(string userId, string updateType);
    Task<ServiceResult> SendSecurityAlertAsync(string userId, string alertMessage);
    Task<ServiceResult> SendPasswordResetNotificationAsync(string userId);
    
    // Communication Management
    Task<ServiceResult> MarkMessageAsReadAsync(int communicationId, string userId);
    Task<ServiceResult> DeleteCommunicationAsync(int communicationId, string adminId);
    Task<int> GetUnreadMessageCountAsync(string userId);
    Task<List<UserCommunicationEntity>> GetRecentCommunicationsAsync(string userId, int count = 5);
} 