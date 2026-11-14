using DocFlowHub.Core.Models.Admin.Browser;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Admin;

/// <summary>
/// Read-only queries for the redesigned admin screens (decision S14-1). Counts are done in the
/// database; the AI log of a period is aggregated in memory (decimal costs and TimeSpan latencies
/// do not translate the same way on SQL Server and SQLite).
/// </summary>
public class AdminBrowserService : IAdminBrowserService
{
    private const int ActionsShown = 8;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminBrowserService> _logger;

    public AdminBrowserService(ApplicationDbContext context, ILogger<AdminBrowserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<AdminNavStats>> GetNavStatsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var month = now.AddDays(-30);
            var week = now.AddDays(-7);
            // Lockout ends are compared in memory: SQLite can't compare DateTimeOffset values.
            var lockouts = await _context.Users.Where(u => u.LockoutEnd != null).Select(u => u.LockoutEnd).ToListAsync();
            var nowOffset = DateTimeOffset.UtcNow;
            return ServiceResult<AdminNavStats>.Success(new AdminNavStats
            {
                Users = await _context.Users.CountAsync(),
                Roles = await _context.Roles.CountAsync(),
                ActiveIn30Days = await _context.Users.CountAsync(u => u.LastLoginAt >= month),
                Locked = lockouts.Count(l => l > nowOffset),
                SignupsIn7Days = await _context.Users.CountAsync(u => u.CreatedAt >= week),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the admin navigation counts");
            return ServiceResult<AdminNavStats>.Failure("The admin counts could not be loaded.");
        }
    }

    public async Task<ServiceResult<Dictionary<string, AdminUserRow>>> GetUserRowsAsync(IReadOnlyCollection<string> userIds)
    {
        try
        {
            var rows = await RowsAsync(userIds.ToList());
            return ServiceResult<Dictionary<string, AdminUserRow>>.Success(rows.ToDictionary(r => r.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the admin user rows");
            return ServiceResult<Dictionary<string, AdminUserRow>>.Failure("The users could not be loaded.");
        }
    }

    public async Task<ServiceResult<AdminUserDetails>> GetUserDetailsAsync(string userId)
    {
        try
        {
            var row = (await RowsAsync(new List<string> { userId })).FirstOrDefault();
            if (row == null)
            {
                return ServiceResult<AdminUserDetails>.Failure("User not found.");
            }

            var details = new AdminUserDetails
            {
                Id = row.Id, Name = row.Name, Email = row.Email, Roles = row.Roles, IsActive = row.IsActive,
                IsLocked = row.IsLocked, EmailConfirmed = row.EmailConfirmed, Teams = row.Teams,
                StorageBytes = row.StorageBytes, DocumentCount = row.DocumentCount, AiRequests = row.AiRequests,
                LastLoginAt = row.LastLoginAt, CreatedAt = row.CreatedAt,
                VersionCount = await _context.DocumentVersions.CountAsync(v => v.UserId == userId),
                SummaryCount = await _context.Documents.CountAsync(d => d.OwnerId == userId && _context.DocumentSummaries.Any(s => s.DocumentId == d.Id)),
            };
            details.RecentActions = await ActionsAsync(_context.UserActivityLogs.Where(l => l.ResourceId == userId || l.UserId == userId), 10);
            return ServiceResult<AdminUserDetails>.Success(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load admin details of user {UserId}", userId);
            return ServiceResult<AdminUserDetails>.Failure("The user could not be loaded.");
        }
    }

    public async Task<ServiceResult<AdminAnalytics>> GetAnalyticsAsync(int days)
    {
        try
        {
            days = days is 7 or 90 ? days : 30;
            var today = DateTime.UtcNow.Date;
            var since = today.AddDays(-(days - 1));
            var analytics = new AdminAnalytics
            {
                Days = days,
                ActiveUsers = await _context.Users.CountAsync(u => u.LastLoginAt >= since),
                StoredBytes = await _context.Documents.SumAsync(d => (long?)d.FileSize) ?? 0,
            };

            var uploads = await _context.DocumentVersions
                .Where(v => v.CreatedAt >= since)
                .Select(v => v.CreatedAt)
                .ToListAsync();
            var perDay = uploads.GroupBy(d => d.Date).ToDictionary(g => g.Key, g => g.Count());
            analytics.Uploads = uploads.Count;
            analytics.UploadsPerDay = Enumerable.Range(0, days)
                .Select(i => since.AddDays(i))
                .Select(day => new DailyCount { Day = day, Count = perDay.GetValueOrDefault(day) })
                .ToList();

            var logs = await _context.AIUsageLogs
                .Where(l => l.CreatedAt >= since)
                .Select(l => new { l.OperationType, l.AIModel, l.EstimatedCost, l.IsSuccess, l.ServedFromCache, l.ResponseTime })
                .ToListAsync();
            analytics.AiRequests = logs.Count;
            analytics.AiCost = logs.Sum(l => l.EstimatedCost);
            if (logs.Count > 0)
            {
                analytics.AiByOperation = logs
                    .GroupBy(l => l.OperationType)
                    .Select(g => new AiOperationUsage
                    {
                        Operation = g.Key,
                        Requests = g.Count(),
                        Cost = g.Sum(l => l.EstimatedCost),
                        Share = (double)g.Count() / logs.Count,
                    })
                    .OrderByDescending(o => o.Requests)
                    .ToList();
                analytics.TopModel = logs.GroupBy(l => l.AIModel).OrderByDescending(g => g.Count()).First().Key;
                analytics.CacheHitRate = (double)logs.Count(l => l.ServedFromCache) / logs.Count;
                analytics.FailureRate = (double)logs.Count(l => !l.IsSuccess) / logs.Count;
                var latencies = logs.Select(l => l.ResponseTime.TotalSeconds).OrderBy(s => s).ToList();
                analytics.P95LatencySeconds = latencies[Math.Min(latencies.Count - 1, (int)Math.Ceiling(latencies.Count * 0.95) - 1)];
            }

            var storage = await _context.Documents
                .GroupBy(d => d.OwnerId)
                .Select(g => new { UserId = g.Key, Bytes = g.Sum(d => d.FileSize), Docs = g.Count() })
                .OrderByDescending(x => x.Bytes)
                .Take(6)
                .ToListAsync();
            var names = await NamesAsync(storage.Select(s => s.UserId));
            analytics.TopStorage = storage
                .Select(s => new StorageUser { UserId = s.UserId, Name = names.GetValueOrDefault(s.UserId) ?? "Unknown", Bytes = s.Bytes, Documents = s.Docs })
                .ToList();

            analytics.AdminActions = await ActionsAsync(_context.UserActivityLogs.Where(l => l.Timestamp >= since), ActionsShown);
            return ServiceResult<AdminAnalytics>.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load admin analytics for {Days} days", days);
            return ServiceResult<AdminAnalytics>.Failure("The analytics could not be loaded.");
        }
    }

    public async Task<ServiceResult<UserAiUsage>> GetUserAiUsageAsync(string userId)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var month = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var since = today.AddDays(-13);
            var from = month < since ? month : since;
            var dates = await _context.AIUsageLogs
                .Where(l => l.UserId == userId && l.CreatedAt >= from)
                .Select(l => l.CreatedAt)
                .ToListAsync();
            var perDay = dates.GroupBy(d => d.Date).ToDictionary(g => g.Key, g => g.Count());
            return ServiceResult<UserAiUsage>.Success(new UserAiUsage
            {
                RequestsThisMonth = dates.Count(d => d >= month),
                RequestsToday = dates.Count(d => d >= today),
                Last14Days = Enumerable.Range(0, 14)
                    .Select(i => since.AddDays(i))
                    .Select(day => new DailyCount { Day = day, Count = perDay.GetValueOrDefault(day) })
                    .ToList(),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the AI usage of user {UserId}", userId);
            return ServiceResult<UserAiUsage>.Failure("The AI usage could not be loaded.");
        }
    }

    public async Task<ServiceResult<List<AiUsageToday>>> GetAiUsageTodayAsync()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var hour = today.AddHours(DateTime.UtcNow.Hour);
            var logs = await _context.AIUsageLogs
                .Where(l => l.CreatedAt >= today)
                .Select(l => new { l.UserId, l.CreatedAt, l.EstimatedCost })
                .ToListAsync();
            var byUser = logs.GroupBy(l => l.UserId).ToDictionary(g => g.Key, g => g.ToList());

            var users = await _context.Users
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                .ToListAsync();
            return ServiceResult<List<AiUsageToday>>.Success(users
                .Select(u =>
                {
                    var own = byUser.GetValueOrDefault(u.Id) ?? new();
                    return new AiUsageToday
                    {
                        UserId = u.Id,
                        Name = DisplayFormat.PersonName(u.FirstName, u.LastName, u.Email),
                        Email = u.Email ?? string.Empty,
                        Requests = own.Count,
                        RequestsThisHour = own.Count(l => l.CreatedAt >= hour),
                        Cost = own.Sum(l => l.EstimatedCost),
                    };
                })
                .OrderByDescending(u => u.Requests)
                .ThenBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
                .ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load today's AI usage");
            return ServiceResult<List<AiUsageToday>>.Failure("The AI usage could not be loaded.");
        }
    }

    private async Task<List<AdminUserRow>> RowsAsync(List<string> ids)
    {
        if (ids.Count == 0) return new List<AdminUserRow>();
        var since = DateTime.UtcNow.AddDays(-30);

        var users = await _context.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new
            {
                u.Id, u.FirstName, u.LastName, u.Email, u.IsActive, u.EmailConfirmed, u.LockoutEnd, u.LastLoginAt, u.CreatedAt,
                Bytes = _context.Documents.Where(d => d.OwnerId == u.Id).Sum(d => (long?)d.FileSize),
                Docs = _context.Documents.Count(d => d.OwnerId == u.Id),
                Ai = _context.AIUsageLogs.Count(l => l.UserId == u.Id && l.CreatedAt >= since),
            })
            .ToListAsync();
        var roles = await _context.UserRoles
            .Where(ur => ids.Contains(ur.UserId))
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();
        var teams = await _context.TeamMembers
            .Where(m => ids.Contains(m.UserId))
            .Select(m => new { m.UserId, m.Team.Name })
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        return users
            .Select(u => new AdminUserRow
            {
                Id = u.Id,
                Name = DisplayFormat.PersonName(u.FirstName, u.LastName, u.Email),
                Email = u.Email ?? string.Empty,
                IsActive = u.IsActive,
                EmailConfirmed = u.EmailConfirmed,
                IsLocked = u.LockoutEnd != null && u.LockoutEnd > now,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
                StorageBytes = u.Bytes ?? 0,
                DocumentCount = u.Docs,
                AiRequests = u.Ai,
                Roles = roles.Where(r => r.UserId == u.Id && r.Name != null).Select(r => r.Name!).OrderBy(n => n).ToList(),
                Teams = teams.Where(t => t.UserId == u.Id).Select(t => t.Name).OrderBy(n => n).ToList(),
            })
            .OrderBy(u => ids.IndexOf(u.Id))
            .ToList();
    }

    private async Task<List<AdminActionRow>> ActionsAsync(IQueryable<Core.Models.Admin.UserActivityLog> logs, int take)
    {
        var rows = await logs
            .OrderByDescending(l => l.Timestamp)
            .Take(take)
            .Select(l => new { l.Timestamp, l.ActivityType, l.Description, l.UserId })
            .ToListAsync();
        var names = await NamesAsync(rows.Select(r => r.UserId));
        return rows.Select(r => new AdminActionRow
        {
            At = r.Timestamp,
            Type = r.ActivityType,
            Description = r.Description,
            ActorName = names.GetValueOrDefault(r.UserId) ?? "Unknown",
        }).ToList();
    }

    private async Task<Dictionary<string, string>> NamesAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.Distinct().ToList();
        return await _context.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToDictionaryAsync(u => u.Id, u => DisplayFormat.PersonName(u.FirstName, u.LastName, u.Email));
    }
}
