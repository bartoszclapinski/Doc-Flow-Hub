namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// User-specific usage statistics for analytics dashboard
/// </summary>
public class UserUsageStatistics
{
    public string UserId { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public int TotalTokensUsed { get; set; }
    public decimal TotalCost { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int OperationsFromCache { get; set; }
    public Dictionary<string, int> OperationTypeBreakdown { get; set; } = new();
    public Dictionary<string, int> ModelUsageBreakdown { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public double ErrorRate => TotalOperations > 0 ? (double)FailedOperations / TotalOperations : 0;
    public double CacheHitRate => TotalOperations > 0 ? (double)OperationsFromCache / TotalOperations : 0;
}

/// <summary>
/// System-wide usage statistics for admin monitoring
/// </summary>
public class SystemUsageStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public long TotalTokensUsed { get; set; }
    public decimal TotalCost { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int OperationsFromCache { get; set; }
    public Dictionary<string, int> OperationTypeBreakdown { get; set; } = new();
    public Dictionary<string, int> ModelUsageBreakdown { get; set; } = new();
    public Dictionary<string, int> DailyOperations { get; set; } = new();
    public List<TopUser> TopUsersByUsage { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public double ErrorRate => TotalOperations > 0 ? (double)FailedOperations / TotalOperations : 0;
    public double CacheHitRate => TotalOperations > 0 ? (double)OperationsFromCache / TotalOperations : 0;
}

/// <summary>
/// Top user by usage for system statistics
/// </summary>
public class TopUser
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int Operations { get; set; }
    public decimal Cost { get; set; }
    public int TokensUsed { get; set; }
}

/// <summary>
/// Cost breakdown analysis for a user
/// </summary>
public class UserCostBreakdown
{
    public string UserId { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public Dictionary<string, decimal> CostByOperationType { get; set; } = new();
    public Dictionary<string, decimal> CostByModel { get; set; } = new();
    public Dictionary<string, decimal> DailyCosts { get; set; } = new();
    public List<CostlyOperation> MostExpensiveOperations { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal AverageCostPerOperation => TotalOperations > 0 ? TotalCost / TotalOperations : 0;
    public int TotalOperations { get; set; }
}

/// <summary>
/// Individual costly operation details
/// </summary>
public class CostlyOperation
{
    public int Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string AIModel { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public decimal Cost { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? DocumentId { get; set; }
    public int InputSize { get; set; }
    public int OutputSize { get; set; }
}

/// <summary>
/// Usage trends for analytics charts
/// </summary>
public class UsageTrends
{
    public Dictionary<string, int> DailyOperations { get; set; } = new();
    public Dictionary<string, decimal> DailyCosts { get; set; } = new();
    public Dictionary<string, int> DailyTokens { get; set; } = new();
    public Dictionary<string, double> DailyAverageResponseTime { get; set; } = new();
    public Dictionary<string, double> DailyErrorRate { get; set; } = new();
    public Dictionary<string, double> DailyCacheHitRate { get; set; } = new();
    public List<string> TrendLabels { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public TrendDirection OperationsTrend { get; set; }
    public TrendDirection CostTrend { get; set; }
    public TrendDirection PerformanceTrend { get; set; }
}

/// <summary>
/// Trend direction for analytics
/// </summary>
public enum TrendDirection
{
    Stable,
    Increasing,
    Decreasing,
    Volatile
}

/// <summary>
/// Expensive operation details for cost optimization
/// </summary>
public class ExpensiveOperation
{
    public string OperationType { get; set; } = string.Empty;
    public string AIModel { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public int OperationCount { get; set; }
    public decimal AverageCost { get; set; }
    public int TotalTokens { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public DateTime FirstOperation { get; set; }
    public DateTime LastOperation { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Error rate statistics for reliability monitoring
/// </summary>
public class ErrorRateStatistics
{
    public double OverallErrorRate { get; set; }
    public Dictionary<string, double> ErrorRateByOperationType { get; set; } = new();
    public Dictionary<string, double> ErrorRateByModel { get; set; } = new();
    public Dictionary<string, int> CommonErrors { get; set; } = new();
    public Dictionary<string, double> DailyErrorRates { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalOperations { get; set; }
    public int TotalErrors { get; set; }
    public List<ErrorPattern> ErrorPatterns { get; set; } = new();
}

/// <summary>
/// Error pattern analysis
/// </summary>
public class ErrorPattern
{
    public string ErrorType { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// User rate limits and current usage
/// </summary>
public class UserRateLimits
{
    public string UserId { get; set; } = string.Empty;
    public int DailyOperationLimit { get; set; }
    public int CurrentDailyOperations { get; set; }
    public decimal DailyCostLimit { get; set; }
    public decimal CurrentDailyCost { get; set; }
    public int HourlyOperationLimit { get; set; }
    public int CurrentHourlyOperations { get; set; }
    public Dictionary<string, int> OperationTypeLimits { get; set; } = new();
    public Dictionary<string, int> CurrentOperationTypeUsage { get; set; } = new();
    public DateTime ResetTime { get; set; }
    public bool IsNearLimit => CurrentDailyOperations >= DailyOperationLimit * 0.8m;
    public bool IsOverLimit => CurrentDailyOperations >= DailyOperationLimit || CurrentDailyCost >= DailyCostLimit;
    public double UsagePercentage => DailyOperationLimit > 0 ? (double)CurrentDailyOperations / DailyOperationLimit : 0;
} 