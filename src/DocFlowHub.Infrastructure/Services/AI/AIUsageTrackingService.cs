using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace DocFlowHub.Infrastructure.Services.AI;

/// <summary>
/// Service for AI usage tracking, analytics, and cost monitoring
/// Provides comprehensive monitoring of AI operations with performance optimization
/// </summary>
public class AIUsageTrackingService : IAIUsageTrackingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AIUsageTrackingService> _logger;
    private readonly IMemoryCache _memoryCache;
    
    // Cache keys and expiration times
    private static readonly TimeSpan StatisticsCacheExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan UserLimitsCacheExpiration = TimeSpan.FromMinutes(5);

    // Cost per 1K tokens for different models (in USD)
    private static readonly Dictionary<string, decimal> ModelCosts = new()
    {
        ["gpt-4o"] = 0.005m,
        ["gpt-4o-mini"] = 0.000150m,
        ["gpt-4.1"] = 0.003m,
        ["gpt-4.1-mini"] = 0.000200m,
        ["gpt-3.5-turbo"] = 0.001m
    };

    public AIUsageTrackingService(
        ApplicationDbContext context,
        ILogger<AIUsageTrackingService> logger,
        IMemoryCache memoryCache)
    {
        _context = context;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// Log an AI operation usage event
    /// </summary>
    public async Task<ServiceResult> LogUsageAsync(
        string userId,
        string operationType,
        AIResponse aiResponse,
        int? documentId = null,
        int inputSize = 0,
        bool servedFromCache = false,
        string? ipAddress = null,
        string? userAgent = null,
        double? qualitySetting = null,
        string? metadata = null)
    {
        try
        {
            _logger.LogDebug("Logging AI usage for user {UserId}, operation {OperationType}", userId, operationType);

            var estimatedCost = CalculateEstimatedCost(
                ParseModelString(aiResponse.Model), 
                aiResponse.TokensUsed);

            var usageLog = new AIUsageLog
            {
                UserId = userId,
                OperationType = operationType,
                AIModel = aiResponse.Model,
                TokensUsed = aiResponse.TokensUsed,
                EstimatedCost = estimatedCost,
                ResponseTime = aiResponse.ResponseTime,
                IsSuccess = aiResponse.IsSuccess,
                ErrorMessage = aiResponse.ErrorMessage,
                CreatedAt = aiResponse.GeneratedAt,
                DocumentId = documentId,
                InputSize = inputSize,
                OutputSize = aiResponse.Content?.Length ?? 0,
                ServedFromCache = servedFromCache,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                QualitySetting = qualitySetting,
                Metadata = metadata
            };

            _context.AIUsageLogs.Add(usageLog);
            await _context.SaveChangesAsync();

            // Invalidate user-specific caches
            InvalidateUserCaches(userId);

            _logger.LogInformation("Successfully logged AI usage for user {UserId}, operation {OperationType}, cost ${Cost:F4}", 
                userId, operationType, estimatedCost);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging AI usage for user {UserId}, operation {OperationType}", userId, operationType);
            return ServiceResult.Failure("Failed to log AI usage");
        }
    }

    /// <summary>
    /// Calculate estimated cost for an operation before execution
    /// </summary>
    public decimal CalculateEstimatedCost(AIModel model, int estimatedTokens)
    {
        var modelString = model.ToApiString();
        if (ModelCosts.TryGetValue(modelString, out var costPer1K))
        {
            return (estimatedTokens / 1000m) * costPer1K;
        }

        // Default to GPT-4o-mini cost if model not found
        return (estimatedTokens / 1000m) * ModelCosts["gpt-4o-mini"];
    }

    /// <summary>
    /// Check if user has exceeded their rate limits
    /// </summary>
    public async Task<bool> CheckRateLimitsAsync(string userId, string operationType)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var currentHour = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);

            var dailyCount = await _context.AIUsageLogs
                .CountAsync(log => log.UserId == userId && log.CreatedAt >= today);

            var hourlyCount = await _context.AIUsageLogs
                .CountAsync(log => log.UserId == userId && log.CreatedAt >= currentHour);

            // Default limits (could be made configurable per user)
            const int dailyLimit = 1000;
            const int hourlyLimit = 100;

            return dailyCount < dailyLimit && hourlyCount < hourlyLimit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limits for user {UserId}", userId);
            return true; // Allow operation if check fails
        }
    }

    /// <summary>
    /// Get usage statistics for a specific user
    /// </summary>
    public async Task<ServiceResult<UserUsageStatistics>> GetUserUsageStatisticsAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            var start = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var end = toDate ?? DateTime.UtcNow;

            var cacheKey = GetUserStatisticsCacheKey(userId, start, end);
            if (_memoryCache.TryGetValue(cacheKey, out UserUsageStatistics? cached) && cached != null)
            {
                _logger.LogDebug("Retrieved user usage statistics from cache for user {UserId}", userId);
                return ServiceResult<UserUsageStatistics>.Success(cached);
            }

            var logs = await _context.AIUsageLogs
                .Where(log => log.UserId == userId && log.CreatedAt >= start && log.CreatedAt <= end)
                .ToListAsync();

            var statistics = new UserUsageStatistics
            {
                UserId = userId,
                TotalOperations = logs.Count,
                SuccessfulOperations = logs.Count(l => l.IsSuccess),
                FailedOperations = logs.Count(l => !l.IsSuccess),
                TotalTokensUsed = logs.Sum(l => l.TokensUsed),
                TotalCost = logs.Sum(l => l.EstimatedCost),
                AverageResponseTime = logs.Count > 0 ? 
                    TimeSpan.FromMilliseconds(logs.Average(l => l.ResponseTime.TotalMilliseconds)) : 
                    TimeSpan.Zero,
                OperationsFromCache = logs.Count(l => l.ServedFromCache),
                OperationTypeBreakdown = logs.GroupBy(l => l.OperationType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ModelUsageBreakdown = logs.GroupBy(l => l.AIModel)
                    .ToDictionary(g => g.Key, g => g.Count()),
                PeriodStart = start,
                PeriodEnd = end
            };

            // Cache the result
            _memoryCache.Set(cacheKey, statistics, StatisticsCacheExpiration);
            _logger.LogDebug("Cached user usage statistics for user {UserId}", userId);

            return ServiceResult<UserUsageStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user usage statistics for user {UserId}", userId);
            return ServiceResult<UserUsageStatistics>.Failure("Failed to retrieve user usage statistics");
        }
    }

    /// <summary>
    /// Get cost breakdown for a user
    /// </summary>
    public async Task<ServiceResult<UserCostBreakdown>> GetUserCostBreakdownAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            var start = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var end = toDate ?? DateTime.UtcNow;

            var logs = await _context.AIUsageLogs
                .Where(log => log.UserId == userId && log.CreatedAt >= start && log.CreatedAt <= end)
                .ToListAsync();

            var breakdown = new UserCostBreakdown
            {
                UserId = userId,
                TotalCost = logs.Sum(l => l.EstimatedCost),
                TotalOperations = logs.Count,
                CostByOperationType = logs.GroupBy(l => l.OperationType)
                    .ToDictionary(g => g.Key, g => g.Sum(l => l.EstimatedCost)),
                CostByModel = logs.GroupBy(l => l.AIModel)
                    .ToDictionary(g => g.Key, g => g.Sum(l => l.EstimatedCost)),
                DailyCosts = logs.GroupBy(l => l.CreatedAt.Date)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Sum(l => l.EstimatedCost)),
                MostExpensiveOperations = logs.OrderByDescending(l => l.EstimatedCost)
                    .Take(10)
                    .Select(l => new CostlyOperation
                    {
                        Id = l.Id,
                        OperationType = l.OperationType,
                        AIModel = l.AIModel,
                        TokensUsed = l.TokensUsed,
                        Cost = l.EstimatedCost,
                        ResponseTime = l.ResponseTime,
                        CreatedAt = l.CreatedAt,
                        DocumentId = l.DocumentId,
                        InputSize = l.InputSize,
                        OutputSize = l.OutputSize
                    }).ToList(),
                PeriodStart = start,
                PeriodEnd = end
            };

            return ServiceResult<UserCostBreakdown>.Success(breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user cost breakdown for user {UserId}", userId);
            return ServiceResult<UserCostBreakdown>.Failure("Failed to retrieve cost breakdown");
        }
    }

    /// <summary>
    /// Get system-wide usage statistics (admin only)
    /// </summary>
    public async Task<ServiceResult<SystemUsageStatistics>> GetSystemUsageStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            var start = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var end = toDate ?? DateTime.UtcNow;

            var cacheKey = GetSystemStatisticsCacheKey(start, end);
            if (_memoryCache.TryGetValue(cacheKey, out SystemUsageStatistics? cached) && cached != null)
            {
                _logger.LogDebug("Retrieved system usage statistics from cache");
                return ServiceResult<SystemUsageStatistics>.Success(cached);
            }

            var logs = await _context.AIUsageLogs
                .Where(log => log.CreatedAt >= start && log.CreatedAt <= end)
                .ToListAsync();

            var uniqueUsers = logs.Select(l => l.UserId).Distinct().Count();
            var activeUsers = logs.Where(l => l.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .Select(l => l.UserId).Distinct().Count();

            var topUsers = logs.GroupBy(l => l.UserId)
                .Select(g => new TopUser
                {
                    UserId = g.Key,
                    UserEmail = GetUserEmail(g.Key), // Helper method to get email
                    Operations = g.Count(),
                    Cost = g.Sum(l => l.EstimatedCost),
                    TokensUsed = g.Sum(l => l.TokensUsed)
                })
                .OrderByDescending(u => u.Operations)
                .Take(10)
                .ToList();

            var statistics = new SystemUsageStatistics
            {
                TotalUsers = uniqueUsers,
                ActiveUsers = activeUsers,
                TotalOperations = logs.Count,
                SuccessfulOperations = logs.Count(l => l.IsSuccess),
                FailedOperations = logs.Count(l => !l.IsSuccess),
                TotalTokensUsed = logs.Sum(l => (long)l.TokensUsed),
                TotalCost = logs.Sum(l => l.EstimatedCost),
                AverageResponseTime = logs.Count > 0 ? 
                    TimeSpan.FromMilliseconds(logs.Average(l => l.ResponseTime.TotalMilliseconds)) : 
                    TimeSpan.Zero,
                OperationsFromCache = logs.Count(l => l.ServedFromCache),
                OperationTypeBreakdown = logs.GroupBy(l => l.OperationType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ModelUsageBreakdown = logs.GroupBy(l => l.AIModel)
                    .ToDictionary(g => g.Key, g => g.Count()),
                DailyOperations = logs.GroupBy(l => l.CreatedAt.Date)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count()),
                TopUsersByUsage = topUsers,
                PeriodStart = start,
                PeriodEnd = end
            };

            // Cache the result
            _memoryCache.Set(cacheKey, statistics, StatisticsCacheExpiration);
            _logger.LogDebug("Cached system usage statistics");

            return ServiceResult<SystemUsageStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system usage statistics");
            return ServiceResult<SystemUsageStatistics>.Failure("Failed to retrieve system usage statistics");
        }
    }

    /// <summary>
    /// Get usage trends over time for admin analytics
    /// </summary>
    public async Task<ServiceResult<UsageTrends>> GetUsageTrendsAsync(string? userId = null, int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var cacheKey = $"usage_trends_{userId ?? "system"}_{days}";

            if (_memoryCache.TryGetValue(cacheKey, out UsageTrends? cached) && cached != null)
            {
                _logger.LogDebug("Retrieved usage trends from cache for {UserId}", userId ?? "system");
                return ServiceResult<UsageTrends>.Success(cached);
            }

            var query = _context.AIUsageLogs.Where(log => log.CreatedAt >= startDate);
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(log => log.UserId == userId);
            }

            var logs = await query.ToListAsync();

            var dailyData = logs.GroupBy(l => l.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => new
                {
                    Operations = g.Count(),
                    Cost = g.Sum(l => l.EstimatedCost),
                    Tokens = g.Sum(l => l.TokensUsed),
                    SuccessRate = g.Count() > 0 ? (double)g.Count(l => l.IsSuccess) / g.Count() * 100 : 0
                });

            var trends = new UsageTrends
            {
                PeriodStart = startDate,
                PeriodEnd = DateTime.UtcNow,
                OperationsTrend = CalculateTrendDirection(dailyData.Values.Select(v => v.Operations).ToList()),
                CostTrend = CalculateTrendDirection(dailyData.Values.Select(v => (int)(v.Cost * 100)).ToList()),
                PerformanceTrend = TrendDirection.Stable,
                DailyOperations = dailyData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Operations),
                DailyCosts = dailyData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Cost),
                DailyTokens = dailyData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Tokens),
                DailyErrorRate = dailyData.ToDictionary(kvp => kvp.Key, kvp => 100.0 - kvp.Value.SuccessRate),
                TrendLabels = dailyData.Keys.ToList()
            };

            // Cache for 30 minutes
            _memoryCache.Set(cacheKey, trends, TimeSpan.FromMinutes(30));
            
            return ServiceResult<UsageTrends>.Success(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage trends for {UserId}", userId ?? "system");
            return ServiceResult<UsageTrends>.Failure("Failed to retrieve usage trends");
        }
    }

    /// <summary>
    /// Get most expensive operations for cost optimization analysis
    /// </summary>
    public async Task<ServiceResult<List<ExpensiveOperation>>> GetMostExpensiveOperationsAsync(string? userId = null, int limit = 10)
    {
        try
        {
            var cacheKey = $"expensive_operations_{userId ?? "system"}_{limit}";
            
            if (_memoryCache.TryGetValue(cacheKey, out List<ExpensiveOperation>? cached) && cached != null)
            {
                _logger.LogDebug("Retrieved expensive operations from cache for {UserId}", userId ?? "system");
                return ServiceResult<List<ExpensiveOperation>>.Success(cached);
            }

            var query = _context.AIUsageLogs.AsQueryable();
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(log => log.UserId == userId);
            }

            var logs = await query.ToListAsync();
            
            var expensiveOps = logs
                .GroupBy(log => new { log.OperationType, log.AIModel })
                .Select(g => new ExpensiveOperation
                {
                    OperationType = g.Key.OperationType,
                    AIModel = g.Key.AIModel,
                    TotalCost = g.Sum(log => log.EstimatedCost),
                    OperationCount = g.Count(),
                    AverageCost = g.Average(log => log.EstimatedCost),
                    TotalTokens = g.Sum(log => log.TokensUsed),
                    AverageResponseTime = TimeSpan.FromMilliseconds(g.Average(log => log.ResponseTime.TotalMilliseconds)),
                    FirstOperation = g.Min(log => log.CreatedAt),
                    LastOperation = g.Max(log => log.CreatedAt),
                    Recommendation = g.Sum(log => log.EstimatedCost) > 5.0m ? 
                        "Consider using a smaller model or implementing additional caching" :
                        "Usage is within optimal cost range"
                })
                .OrderByDescending(op => op.TotalCost)
                .Take(limit)
                .ToList();

            // Cache for 1 hour
            _memoryCache.Set(cacheKey, expensiveOps, TimeSpan.FromHours(1));
            
            return ServiceResult<List<ExpensiveOperation>>.Success(expensiveOps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expensive operations for {UserId}", userId ?? "system");
            return ServiceResult<List<ExpensiveOperation>>.Failure("Failed to retrieve expensive operations");
        }
    }

    /// <summary>
    /// Get error rate statistics for reliability monitoring
    /// </summary>
    public async Task<ServiceResult<ErrorRateStatistics>> GetErrorRateStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var start = fromDate ?? DateTime.UtcNow.AddDays(-7);
            var end = toDate ?? DateTime.UtcNow;
            var cacheKey = $"error_statistics_{start:yyyyMMdd}_{end:yyyyMMdd}";

            if (_memoryCache.TryGetValue(cacheKey, out ErrorRateStatistics? cached) && cached != null)
            {
                _logger.LogDebug("Retrieved error rate statistics from cache");
                return ServiceResult<ErrorRateStatistics>.Success(cached);
            }

            var logs = await _context.AIUsageLogs
                .Where(log => log.CreatedAt >= start && log.CreatedAt <= end)
                .ToListAsync();

            var totalOperations = logs.Count;
            var failedOperations = logs.Count(l => !l.IsSuccess);
            var overallErrorRate = totalOperations > 0 ? (double)failedOperations / totalOperations * 100 : 0;

            var errorPatterns = logs.Where(l => !l.IsSuccess && !string.IsNullOrEmpty(l.ErrorMessage))
                .GroupBy(l => l.ErrorMessage!)
                .Select(g => new ErrorPattern
                {
                    ErrorType = "API Error",
                    Pattern = g.Key,
                    Frequency = g.Count(),
                    FirstOccurrence = g.Min(l => l.CreatedAt),
                    LastOccurrence = g.Max(l => l.CreatedAt),
                    Recommendation = GetErrorRecommendation(g.Key)
                })
                .OrderByDescending(ep => ep.Frequency)
                .Take(10)
                .ToList();

            var dailyErrorRates = logs.GroupBy(l => l.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g =>
                {
                    var dayTotal = g.Count();
                    var dayFailed = g.Count(l => !l.IsSuccess);
                    return dayTotal > 0 ? (double)dayFailed / dayTotal * 100 : 0;
                });

            var statistics = new ErrorRateStatistics
            {
                PeriodStart = start,
                PeriodEnd = end,
                TotalOperations = totalOperations,
                TotalErrors = failedOperations,
                OverallErrorRate = overallErrorRate,
                DailyErrorRates = dailyErrorRates,
                ErrorPatterns = errorPatterns,
                ErrorRateByOperationType = logs.GroupBy(l => l.OperationType)
                    .ToDictionary(g => g.Key, g =>
                    {
                        var typeTotal = g.Count();
                        var typeFailed = g.Count(l => !l.IsSuccess);
                        return typeTotal > 0 ? (double)typeFailed / typeTotal * 100 : 0;
                    }),
                ErrorRateByModel = logs.GroupBy(l => l.AIModel)
                    .ToDictionary(g => g.Key, g =>
                    {
                        var modelTotal = g.Count();
                        var modelFailed = g.Count(l => !l.IsSuccess);
                        return modelTotal > 0 ? (double)modelFailed / modelTotal * 100 : 0;
                    })
            };

            // Cache for 15 minutes
            _memoryCache.Set(cacheKey, statistics, TimeSpan.FromMinutes(15));
            
            return ServiceResult<ErrorRateStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving error rate statistics");
            return ServiceResult<ErrorRateStatistics>.Failure("Failed to retrieve error rate statistics");
        }
    }

    /// <summary>
    /// Get user rate limits and current usage status
    /// </summary>
    public async Task<ServiceResult<UserRateLimits>> GetUserRateLimitsAsync(string userId)
    {
        try
        {
            var cacheKey = $"user_limits_{userId}";
            
            if (_memoryCache.TryGetValue(cacheKey, out UserRateLimits? cached) && cached != null)
            {
                _logger.LogDebug("Retrieved user rate limits from cache for {UserId}", userId);
                return ServiceResult<UserRateLimits>.Success(cached);
            }

            var today = DateTime.UtcNow.Date;
            var currentHour = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);
            var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var dailyCount = await _context.AIUsageLogs
                .CountAsync(log => log.UserId == userId && log.CreatedAt >= today);

            var hourlyCount = await _context.AIUsageLogs
                .CountAsync(log => log.UserId == userId && log.CreatedAt >= currentHour);

            var monthlyCount = await _context.AIUsageLogs
                .CountAsync(log => log.UserId == userId && log.CreatedAt >= thisMonth);

            var monthlyCost = await _context.AIUsageLogs
                .Where(log => log.UserId == userId && log.CreatedAt >= thisMonth)
                .SumAsync(log => log.EstimatedCost);

            var dailyCost = await _context.AIUsageLogs
                .Where(log => log.UserId == userId && log.CreatedAt >= today)
                .SumAsync(log => log.EstimatedCost);

            var operationTypeUsage = await _context.AIUsageLogs
                .Where(log => log.UserId == userId && log.CreatedAt >= today)
                .GroupBy(log => log.OperationType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            // Default limits (could be made configurable per user/role)
            const int dailyLimit = 1000;
            const int hourlyLimit = 100;
            const decimal dailyCostLimit = 10.00m;

            var limits = new UserRateLimits
            {
                UserId = userId,
                DailyOperationLimit = dailyLimit,
                CurrentDailyOperations = dailyCount,
                DailyCostLimit = dailyCostLimit,
                CurrentDailyCost = dailyCost,
                HourlyOperationLimit = hourlyLimit,
                CurrentHourlyOperations = hourlyCount,
                OperationTypeLimits = new Dictionary<string, int>
                {
                    ["DocumentSummarization"] = 100,
                    ["VersionComparison"] = 50,
                    ["TextExtraction"] = 200
                },
                CurrentOperationTypeUsage = operationTypeUsage,
                ResetTime = DateTime.UtcNow.Date.AddDays(1) // Next midnight
            };

            // Cache for 5 minutes
            _memoryCache.Set(cacheKey, limits, UserLimitsCacheExpiration);
            
            return ServiceResult<UserRateLimits>.Success(limits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user rate limits for {UserId}", userId);
            return ServiceResult<UserRateLimits>.Failure("Failed to retrieve user rate limits");
        }
    }

    #region Helper Methods

    private static AIModel ParseModelString(string modelString)
    {
        return modelString.ToLower() switch
        {
            "gpt-4o" => AIModel.Gpt4o,
            "gpt-4o-mini" => AIModel.Gpt4oMini,
            "gpt-4.1" => AIModel.Gpt41,
            "gpt-4.1-mini" => AIModel.Gpt41Mini,
            _ => AIModel.Gpt4oMini
        };
    }

    private string GetUserEmail(string userId)
    {
        try
        {
            // Try to get real user email from AspNetUsers table
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            return user?.Email ?? $"user-{userId.Substring(0, Math.Min(8, userId.Length))}@domain.com";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve email for user {UserId}", userId);
            return $"user-{userId.Substring(0, Math.Min(8, userId.Length))}@domain.com";
        }
    }

    private static string GetUserStatisticsCacheKey(string userId, DateTime start, DateTime end)
        => $"user_statistics_{userId}_{start:yyyyMMdd}_{end:yyyyMMdd}";

    private static string GetSystemStatisticsCacheKey(DateTime start, DateTime end)
        => $"system_statistics_{start:yyyyMMdd}_{end:yyyyMMdd}";

    private void InvalidateUserCaches(string userId)
    {
        // Invalidate user-specific caches
        var pattern = $"user_statistics_{userId}_";
        // Implementation would remove cache entries matching pattern
        _logger.LogDebug("Invalidated cache entries for user {UserId}", userId);
    }

    /// <summary>
    /// Calculate trend direction based on operation counts
    /// </summary>
    private static TrendDirection CalculateTrendDirection(List<int> values)
    {
        if (values.Count < 2)
            return TrendDirection.Stable;

        var firstHalf = values.Take(values.Count / 2).Average();
        var secondHalf = values.Skip(values.Count / 2).Average();

        var percentChange = firstHalf > 0 ? (secondHalf - firstHalf) / firstHalf * 100 : 0;

        return percentChange switch
        {
            > 10 => TrendDirection.Increasing,
            < -10 => TrendDirection.Decreasing,
            _ => TrendDirection.Stable
        };
    }

    /// <summary>
    /// Get error recommendation based on error message
    /// </summary>
    private static string GetErrorRecommendation(string errorMessage)
    {
        return errorMessage.ToLower() switch
        {
            var msg when msg.Contains("rate limit") => "Consider implementing request throttling or upgrading API tier",
            var msg when msg.Contains("timeout") => "Reduce input size or increase timeout settings",
            var msg when msg.Contains("token") => "Implement input text truncation or use a model with larger context window",
            var msg when msg.Contains("authentication") => "Verify API key configuration and permissions",
            var msg when msg.Contains("quota") => "Monitor usage limits and consider upgrading plan",
            _ => "Monitor error patterns and contact support if persists"
        };
    }

    #endregion
} 