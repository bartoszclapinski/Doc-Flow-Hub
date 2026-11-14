using DocFlowHub.Core.Models.Admin.Browser;
using DocFlowHub.Core.Models.Common;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>Read-only queries behind the redesigned admin screens and the user's AI usage.</summary>
public interface IAdminBrowserService
{
    Task<ServiceResult<AdminNavStats>> GetNavStatsAsync();

    /// <summary>Roles, teams, storage and AI requests for the given users (one page of the users table).</summary>
    Task<ServiceResult<Dictionary<string, AdminUserRow>>> GetUserRowsAsync(IReadOnlyCollection<string> userIds);

    Task<ServiceResult<AdminUserDetails>> GetUserDetailsAsync(string userId);

    /// <summary>Analytics for the last 7, 30 or 90 days.</summary>
    Task<ServiceResult<AdminAnalytics>> GetAnalyticsAsync(int days);

    Task<ServiceResult<UserAiUsage>> GetUserAiUsageAsync(string userId);

    /// <summary>Every user with today's AI requests and cost (users without requests included), busiest first.</summary>
    Task<ServiceResult<List<AiUsageToday>>> GetAiUsageTodayAsync();
}
