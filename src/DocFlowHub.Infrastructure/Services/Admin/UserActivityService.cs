using DocFlowHub.Core.Models.Admin;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DocFlowHub.Infrastructure.Services.Admin;

public class UserActivityService : IUserActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserActivityService> _logger;

    public UserActivityService(ApplicationDbContext context, ILogger<UserActivityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult> LogUserActivityAsync(string userId, string activityType, string description, 
        string? resourceId = null, string? resourceName = null, string ipAddress = "", string userAgent = "", string? additionalData = null)
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
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalData = additionalData,
                Timestamp = DateTime.UtcNow
            };

            _context.UserActivityLogs.Add(activity);
            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging user activity for user {UserId}", userId);
            return ServiceResult.Failure("Error logging activity");
        }
    }

    public async Task<UserActivityReport> GetUserActivityReportAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new UserActivityReport();
            }

            var activityMetrics = await CalculateUserActivityMetricsAsync(userId);
            var usageStatistics = await CalculateUserUsageStatisticsAsync(userId);
            var securityStatus = await GetUserSecurityStatusAsync(userId);
            var recentActivities = await GetUserActivitiesAsync(userId, 20);

            return new UserActivityReport
            {
                UserId = userId,
                Username = user.UserName ?? "",
                Email = user.Email ?? "",
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                ActivityMetrics = activityMetrics,
                UsageStatistics = usageStatistics,
                SecurityStatus = securityStatus,
                RecentActivities = recentActivities
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity report for user {UserId}", userId);
            return new UserActivityReport();
        }
    }

    public async Task<List<UserActivityEntry>> GetUserActivitiesAsync(string userId, int count = 50)
    {
        try
        {
            var activities = await _context.UserActivityLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();

            return activities.Select(a => new UserActivityEntry
            {
                Timestamp = a.Timestamp,
                ActivityType = a.ActivityType,
                Description = a.Description,
                ResourceId = a.ResourceId,
                ResourceName = a.ResourceName,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activities for user {UserId}", userId);
            return new List<UserActivityEntry>();
        }
    }

    public async Task<PagedResult<UserActivityLog>> GetSystemActivitiesAsync(int page = 1, int pageSize = 50, 
        string? userId = null, string? activityType = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var query = _context.UserActivityLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (!string.IsNullOrEmpty(activityType))
            {
                query = query.Where(a => a.ActivityType == activityType);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= toDate.Value);
            }

            var totalCount = await query.CountAsync();
            var activities = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<UserActivityLog>
            {
                Items = activities,
                TotalItems = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system activities");
            return new PagedResult<UserActivityLog>();
        }
    }

    public async Task<UserActivityMetrics> CalculateUserActivityMetricsAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new UserActivityMetrics();
            }

            // Get document-related metrics
            var documentsUploaded = await _context.Documents
                .Where(d => d.OwnerId == userId)
                .CountAsync();

            var documentsShared = 0; // Need to implement sharing functionality
            
            // Get project-related metrics
            var projectsCreated = await _context.Projects
                .Where(p => p.OwnerId == userId)
                .CountAsync();

            var projectsParticipated = await _context.TeamMembers
                .Where(tm => tm.UserId == userId)
                .Select(tm => tm.TeamId)
                .Distinct()
                .CountAsync();

            // Get team-related metrics
            var teamsJoined = await _context.TeamMembers
                .Where(tm => tm.UserId == userId)
                .CountAsync();

            var teamsManaged = await _context.Teams
                .Where(t => t.OwnerId == userId)
                .CountAsync();

            // Get AI operations
            var aiOperationsPerformed = await _context.AIUsageLogs
                .Where(a => a.UserId == userId)
                .CountAsync();

            // Get login metrics
            var loginCount = await _context.UserLoginAttempts
                .Where(la => la.UserId == userId && la.IsSuccessful)
                .CountAsync();

            var activityLevel = await DetermineUserActivityLevelAsync(userId);

            return new UserActivityMetrics
            {
                DocumentsUploaded = documentsUploaded,
                DocumentsDownloaded = 0, // Would need to track this in real implementation
                DocumentsShared = documentsShared,
                ProjectsCreated = projectsCreated,
                ProjectsParticipated = projectsParticipated,
                TeamsJoined = teamsJoined,
                TeamsManaged = teamsManaged,
                AIOperationsPerformed = aiOperationsPerformed,
                LoginCount = loginCount,
                LastLoginAt = user.LastLoginAt,
                LastActivityAt = await GetUserLastActivityAsync(userId),
                ActivityLevel = activityLevel
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating user activity metrics for user {UserId}", userId);
            return new UserActivityMetrics();
        }
    }

    public async Task<ActivityLevel> DetermineUserActivityLevelAsync(string userId)
    {
        try
        {
            var documentCount = await _context.Documents
                .Where(d => d.OwnerId == userId)
                .CountAsync();

            var recentLoginCount = await _context.UserLoginAttempts
                .Where(la => la.UserId == userId && la.IsSuccessful && la.AttemptedAt >= DateTime.UtcNow.AddDays(-30))
                .CountAsync();

            // Determine activity level based on documents and logins
            if (documentCount > 50 || recentLoginCount > 20)
                return ActivityLevel.VeryActive;
            else if (documentCount > 10 || recentLoginCount > 10)
                return ActivityLevel.Active;
            else if (documentCount > 1 || recentLoginCount > 1)
                return ActivityLevel.Moderate;
            else if (documentCount > 0 || recentLoginCount > 0)
                return ActivityLevel.LowActivity;
            else
                return ActivityLevel.Inactive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining activity level for user {UserId}", userId);
            return ActivityLevel.Inactive;
        }
    }

    public async Task<List<UserActivityReport>> GetMostActiveUsersAsync(int count = 10)
    {
        try
        {
            var activeUserIds = await _context.UserActivityLogs
                .Where(a => a.Timestamp >= DateTime.UtcNow.AddDays(-30))
                .GroupBy(a => a.UserId)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .ToListAsync();

            var reports = new List<UserActivityReport>();
            foreach (var userId in activeUserIds)
            {
                var report = await GetUserActivityReportAsync(userId);
                reports.Add(report);
            }

            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting most active users");
            return new List<UserActivityReport>();
        }
    }

    public async Task<List<UserActivityReport>> GetLeastActiveUsersAsync(int count = 10)
    {
        try
        {
            var allUserIds = await _context.Users
                .Select(u => u.Id)
                .ToListAsync();

            var activeUserIds = await _context.UserActivityLogs
                .Where(a => a.Timestamp >= DateTime.UtcNow.AddDays(-30))
                .Select(a => a.UserId)
                .Distinct()
                .ToListAsync();

            var inactiveUserIds = allUserIds
                .Except(activeUserIds)
                .Take(count)
                .ToList();

            var reports = new List<UserActivityReport>();
            foreach (var userId in inactiveUserIds)
            {
                var report = await GetUserActivityReportAsync(userId);
                reports.Add(report);
            }

            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting least active users");
            return new List<UserActivityReport>();
        }
    }

    public async Task<UserUsageStatistics> CalculateUserUsageStatisticsAsync(string userId)
    {
        try
        {
            // Calculate storage usage
            var userDocuments = await _context.Documents
                .Where(d => d.OwnerId == userId)
                .Include(d => d.Versions)
                .ToListAsync();

            var storageUsed = userDocuments
                .SelectMany(d => d.Versions)
                .Sum(v => (long?)v.FileSize) ?? 0;

            // Get AI token usage
            var aiTokensUsed = await _context.AIUsageLogs
                .Where(a => a.UserId == userId)
                .SumAsync(a => a.TokensUsed);

            // Default limits (should come from settings in real implementation)
            const long defaultStorageLimit = 10L * 1024 * 1024 * 1024; // 10GB
            const int defaultAITokenLimit = 100000;

            return new UserUsageStatistics
            {
                StorageUsedBytes = storageUsed,
                StorageLimitBytes = defaultStorageLimit,
                StorageUsagePercentage = defaultStorageLimit > 0 ? (decimal)storageUsed / defaultStorageLimit * 100 : 0,
                BandwidthUsedBytes = 0, // Would need to track this in real implementation
                AITokensUsed = aiTokensUsed,
                AITokensLimit = defaultAITokenLimit,
                AIUsagePercentage = defaultAITokenLimit > 0 ? (decimal)aiTokensUsed / defaultAITokenLimit * 100 : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating user usage statistics for user {UserId}", userId);
            return new UserUsageStatistics();
        }
    }

    public async Task<long> GetUserStorageUsageAsync(string userId)
    {
        try
        {
            var storageUsed = await _context.Documents
                .Where(d => d.OwnerId == userId)
                .Include(d => d.Versions)
                .SelectMany(d => d.Versions)
                .SumAsync(v => v.FileSize);

            return storageUsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage usage for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<int> GetUserAITokenUsageAsync(string userId)
    {
        try
        {
            return await _context.AIUsageLogs
                .Where(a => a.UserId == userId)
                .SumAsync(a => a.TokensUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI token usage for user {UserId}", userId);
            return 0;
        }
    }

    private async Task<DateTime?> GetUserLastActivityAsync(string userId)
    {
        try
        {
            return await _context.UserActivityLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => (DateTime?)a.Timestamp)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last activity for user {UserId}", userId);
            return null;
        }
    }

    private async Task<UserSecurityStatus> GetUserSecurityStatusAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new UserSecurityStatus();
            }

            // Get recent IP addresses
            var recentIps = await _context.UserLoginAttempts
                .Where(la => la.UserId == userId && la.AttemptedAt >= DateTime.UtcNow.AddDays(-30))
                .Select(la => la.IpAddress)
                .Distinct()
                .Take(10)
                .ToListAsync();

            // Get recent devices
            var recentDevices = await _context.UserDevices
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.LastSeen)
                .Take(5)
                .Select(d => $"{d.DeviceType} - {d.Browser}")
                .ToListAsync();

            // Get failed login attempts
            var failedLogins = await _context.UserLoginAttempts
                .Where(la => la.UserId == userId && !la.IsSuccessful && la.AttemptedAt >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();

            // Get security alerts
            var securityAlerts = await _context.UserSecurityEvents
                .Where(se => se.UserId == userId && !se.IsResolved)
                .OrderByDescending(se => se.Timestamp)
                .Take(10)
                .Select(se => new SecurityAlert
                {
                    AlertType = se.EventType,
                    Message = se.Description,
                    Timestamp = se.Timestamp,
                    Severity = se.Severity,
                    IsResolved = se.IsResolved
                })
                .ToListAsync();

            // Calculate security score (simplified)
            var securityScore = 100;
            if (!user.EmailConfirmed) securityScore -= 20;
            if (!user.TwoFactorEnabled) securityScore -= 15;
            if (failedLogins > 3) securityScore -= 10;
            if (securityAlerts.Count > 0) securityScore -= securityAlerts.Count * 5;

            return new UserSecurityStatus
            {
                IsEmailConfirmed = user.EmailConfirmed,
                IsTwoFactorEnabled = user.TwoFactorEnabled,
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                LockoutEnd = user.LockoutEnd?.DateTime,
                FailedLoginAttempts = failedLogins,
                LastPasswordChange = null, // Would need to track this in real implementation
                RequiresPasswordChange = false, // Would need business logic
                RecentIpAddresses = recentIps,
                RecentDevices = recentDevices,
                SecurityScore = Math.Max(0, securityScore),
                SecurityAlerts = securityAlerts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security status for user {UserId}", userId);
            return new UserSecurityStatus();
        }
    }
} 