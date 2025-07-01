using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Common;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Service interface for AI usage tracking and analytics
/// Provides comprehensive monitoring of AI operations, costs, and performance
/// </summary>
public interface IAIUsageTrackingService
{
    /// <summary>
    /// Log an AI operation usage event
    /// </summary>
    /// <param name="userId">User who initiated the operation</param>
    /// <param name="operationType">Type of operation (Summarization, VersionComparison, etc.)</param>
    /// <param name="aiResponse">AI service response with usage data</param>
    /// <param name="documentId">Document ID if operation was document-related</param>
    /// <param name="inputSize">Size of input content in characters</param>
    /// <param name="servedFromCache">Whether response was served from cache</param>
    /// <param name="ipAddress">User's IP address</param>
    /// <param name="userAgent">User's browser user agent</param>
    /// <param name="qualitySetting">Quality/temperature setting used</param>
    /// <param name="metadata">Additional metadata as JSON string</param>
    /// <returns>ServiceResult indicating success or failure</returns>
    Task<ServiceResult> LogUsageAsync(
        string userId,
        string operationType,
        AIResponse aiResponse,
        int? documentId = null,
        int inputSize = 0,
        bool servedFromCache = false,
        string? ipAddress = null,
        string? userAgent = null,
        double? qualitySetting = null,
        string? metadata = null);

    /// <summary>
    /// Get usage statistics for a specific user
    /// </summary>
    /// <param name="userId">User ID to get statistics for</param>
    /// <param name="fromDate">Start date for statistics (optional)</param>
    /// <param name="toDate">End date for statistics (optional)</param>
    /// <returns>ServiceResult with usage statistics</returns>
    Task<ServiceResult<UserUsageStatistics>> GetUserUsageStatisticsAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Get system-wide usage statistics (admin only)
    /// </summary>
    /// <param name="fromDate">Start date for statistics (optional)</param>
    /// <param name="toDate">End date for statistics (optional)</param>
    /// <returns>ServiceResult with system usage statistics</returns>
    Task<ServiceResult<SystemUsageStatistics>> GetSystemUsageStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Get cost breakdown for a user
    /// </summary>
    /// <param name="userId">User ID to get cost breakdown for</param>
    /// <param name="fromDate">Start date for cost analysis (optional)</param>
    /// <param name="toDate">End date for cost analysis (optional)</param>
    /// <returns>ServiceResult with cost breakdown</returns>
    Task<ServiceResult<UserCostBreakdown>> GetUserCostBreakdownAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Get usage trends for analytics dashboard
    /// </summary>
    /// <param name="userId">User ID for user-specific trends, null for system trends</param>
    /// <param name="days">Number of days to analyze (default 30)</param>
    /// <returns>ServiceResult with usage trends data</returns>
    Task<ServiceResult<UsageTrends>> GetUsageTrendsAsync(
        string? userId = null,
        int days = 30);

    /// <summary>
    /// Get most expensive operations for cost optimization insights
    /// </summary>
    /// <param name="userId">User ID for user-specific data, null for system-wide</param>
    /// <param name="limit">Number of operations to return (default 10)</param>
    /// <returns>ServiceResult with expensive operations list</returns>
    Task<ServiceResult<List<ExpensiveOperation>>> GetMostExpensiveOperationsAsync(
        string? userId = null,
        int limit = 10);

    /// <summary>
    /// Get error rate statistics for reliability monitoring
    /// </summary>
    /// <param name="fromDate">Start date for error analysis (optional)</param>
    /// <param name="toDate">End date for error analysis (optional)</param>
    /// <returns>ServiceResult with error rate statistics</returns>
    Task<ServiceResult<ErrorRateStatistics>> GetErrorRateStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Calculate estimated cost for an operation before execution
    /// </summary>
    /// <param name="model">AI model to be used</param>
    /// <param name="estimatedTokens">Estimated number of tokens</param>
    /// <returns>Estimated cost in USD</returns>
    decimal CalculateEstimatedCost(AIModel model, int estimatedTokens);

    /// <summary>
    /// Get current rate limits for a user
    /// </summary>
    /// <param name="userId">User ID to check limits for</param>
    /// <returns>ServiceResult with current usage and limits</returns>
    Task<ServiceResult<UserRateLimits>> GetUserRateLimitsAsync(string userId);

    /// <summary>
    /// Check if user has exceeded their rate limits
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <param name="operationType">Type of operation being attempted</param>
    /// <returns>True if user can proceed, false if rate limited</returns>
    Task<bool> CheckRateLimitsAsync(string userId, string operationType);
} 