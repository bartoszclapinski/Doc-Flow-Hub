using DocFlowHub.Core.Models.Analytics;
using DocFlowHub.Core.Models.Common;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Service interface for comprehensive system analytics and reporting
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Get comprehensive system analytics overview
    /// </summary>
    Task<ServiceResult<SystemAnalytics>> GetSystemAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Get analytics trends for the specified period
    /// </summary>
    Task<ServiceResult<AnalyticsTrends>> GetAnalyticsTrendsAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Get performance analytics for system monitoring
    /// </summary>
    Task<ServiceResult<PerformanceAnalytics>> GetPerformanceAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Get user activity analytics
    /// </summary>
    Task<ServiceResult<List<UserActivity>>> GetUserActivityAnalyticsAsync(int topCount = 10, DateTime? fromDate = null);

    /// <summary>
    /// Get project activity analytics
    /// </summary>
    Task<ServiceResult<List<ProjectActivity>>> GetProjectActivityAnalyticsAsync(int topCount = 10, DateTime? fromDate = null);

    /// <summary>
    /// Get document category usage statistics
    /// </summary>
    Task<ServiceResult<List<CategoryUsage>>> GetCategoryUsageAnalyticsAsync(int topCount = 10);

    /// <summary>
    /// Get daily metrics for the specified date range
    /// </summary>
    Task<ServiceResult<List<DailyMetrics>>> GetDailyMetricsAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Get weekly metrics for the specified date range
    /// </summary>
    Task<ServiceResult<List<WeeklyMetrics>>> GetWeeklyMetricsAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Get monthly metrics for the specified date range
    /// </summary>
    Task<ServiceResult<List<MonthlyMetrics>>> GetMonthlyMetricsAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Get storage usage analytics
    /// </summary>
    Task<ServiceResult<StorageAnalytics>> GetStorageAnalyticsAsync();

    /// <summary>
    /// Get team collaboration analytics
    /// </summary>
    Task<ServiceResult<TeamCollaborationAnalytics>> GetTeamCollaborationAnalyticsAsync(DateTime? fromDate = null);

    /// <summary>
    /// Get AI usage summary analytics
    /// </summary>
    Task<ServiceResult<AIUsageAnalytics>> GetAIUsageAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Generate analytics report for export
    /// </summary>
    Task<ServiceResult<byte[]>> GenerateAnalyticsReportAsync(string format, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Record system event for analytics tracking
    /// </summary>
    Task<ServiceResult<bool>> RecordSystemEventAsync(string eventType, string eventData, string? userId = null);

    /// <summary>
    /// Get real-time dashboard metrics
    /// </summary>
    Task<ServiceResult<RealTimeDashboard>> GetRealTimeDashboardAsync();
}

/// <summary>
/// Storage usage analytics
/// </summary>
public class StorageAnalytics
{
    public long TotalStorageUsed { get; set; }
    public long StorageLimit { get; set; }
    public double StorageUsagePercentage { get; set; }
    public List<UserStorageUsage> UserStorageBreakdown { get; set; } = new();
    public List<ProjectStorageUsage> ProjectStorageBreakdown { get; set; } = new();
    public List<FileTypeUsage> FileTypeBreakdown { get; set; } = new();
    
    // Growth predictions
    public long PredictedUsageNextMonth { get; set; }
    public DateTime EstimatedFullDate { get; set; }
    
    // Calculated properties
    public string TotalStorageUsedFormatted => FormatBytes(TotalStorageUsed);
    public string StorageLimitFormatted => FormatBytes(StorageLimit);
    public string StorageUsageFormatted => $"{StorageUsagePercentage:F2}%";
    public string PredictedUsageFormatted => FormatBytes(PredictedUsageNextMonth);
    
    private static string FormatBytes(long bytes)
    {
        const int scale = 1024;
        string[] orders = { "GB", "MB", "KB", "Bytes" };
        long max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (string order in orders)
        {
            if (bytes > max)
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            
            max /= scale;
        }
        return "0 Bytes";
    }
}

public class UserStorageUsage
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public long StorageUsed { get; set; }
    public int DocumentCount { get; set; }
    public string StorageFormatted => FormatBytes(StorageUsed);
    
    private static string FormatBytes(long bytes)
    {
        const int scale = 1024;
        string[] orders = { "GB", "MB", "KB", "Bytes" };
        long max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (string order in orders)
        {
            if (bytes > max)
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            
            max /= scale;
        }
        return "0 Bytes";
    }
}

public class ProjectStorageUsage
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public long StorageUsed { get; set; }
    public int DocumentCount { get; set; }
    public string StorageFormatted => FormatBytes(StorageUsed);
    
    private static string FormatBytes(long bytes)
    {
        const int scale = 1024;
        string[] orders = { "GB", "MB", "KB", "Bytes" };
        long max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (string order in orders)
        {
            if (bytes > max)
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            
            max /= scale;
        }
        return "0 Bytes";
    }
}

public class FileTypeUsage
{
    public string FileType { get; set; } = string.Empty;
    public long StorageUsed { get; set; }
    public int DocumentCount { get; set; }
    public double Percentage { get; set; }
    public string StorageFormatted => FormatBytes(StorageUsed);
    
    private static string FormatBytes(long bytes)
    {
        const int scale = 1024;
        string[] orders = { "GB", "MB", "KB", "Bytes" };
        long max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (string order in orders)
        {
            if (bytes > max)
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            
            max /= scale;
        }
        return "0 Bytes";
    }
}

/// <summary>
/// Team collaboration analytics
/// </summary>
public class TeamCollaborationAnalytics
{
    public int TotalTeams { get; set; }
    public int ActiveTeams { get; set; }
    public double AverageTeamSize { get; set; }
    public int TotalCollaborations { get; set; }
    public List<TeamActivityMetrics> TeamMetrics { get; set; } = new();
    public List<CollaborationPattern> CollaborationPatterns { get; set; } = new();
}

public class TeamActivityMetrics
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int DocumentsShared { get; set; }
    public int ProjectsCreated { get; set; }
    public DateTime LastActivity { get; set; }
    public int ActivityScore { get; set; }
}

public class CollaborationPattern
{
    public string PatternType { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public double Percentage { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// AI usage analytics
/// </summary>
public class AIUsageAnalytics
{
    public int TotalOperations { get; set; }
    public decimal TotalCost { get; set; }
    public int DocumentSummaries { get; set; }
    public int VersionComparisons { get; set; }
    public double AverageOperationCost { get; set; }
    public List<AIOperationMetrics> OperationBreakdown { get; set; } = new();
    public List<UserAIUsage> UserUsageBreakdown { get; set; } = new();
    
    // Calculated properties
    public string TotalCostFormatted => $"${TotalCost:F2}";
    public string AverageOperationCostFormatted => $"${AverageOperationCost:F4}";
}

public class AIOperationMetrics
{
    public string OperationType { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Cost { get; set; }
    public double Percentage { get; set; }
    public string CostFormatted => $"${Cost:F2}";
}

public class UserAIUsage
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Operations { get; set; }
    public decimal Cost { get; set; }
    public string CostFormatted => $"${Cost:F2}";
}

/// <summary>
/// Real-time dashboard metrics
/// </summary>
public class RealTimeDashboard
{
    public int OnlineUsers { get; set; }
    public int ActiveSessions { get; set; }
    public int TodayUploads { get; set; }
    public int TodayAIOperations { get; set; }
    public double CurrentResponseTime { get; set; }
    public double SystemCpuUsage { get; set; }
    public double SystemMemoryUsage { get; set; }
    public int QueuedJobs { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Recent activities (last 10)
    public List<RecentActivity> RecentActivities { get; set; } = new();
}

public class RecentActivity
{
    public DateTime Timestamp { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss");
} 