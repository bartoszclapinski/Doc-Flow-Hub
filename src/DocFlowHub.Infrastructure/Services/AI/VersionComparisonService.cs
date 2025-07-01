using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace DocFlowHub.Infrastructure.Services.AI;

/// <summary>
/// Service for AI-powered document version comparison and change analysis
/// </summary>
public class VersionComparisonService : IVersionComparisonService
{
    private readonly ApplicationDbContext _context;
    private readonly IAIService _aiService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly IAIUsageTrackingService _usageTrackingService;
    private readonly ILogger<VersionComparisonService> _logger;
    private readonly IMemoryCache _memoryCache;

    // Cache expiration times
    private static readonly TimeSpan ComparisonCacheExpiration = TimeSpan.FromHours(24);
    private static readonly TimeSpan ContentCacheExpiration = TimeSpan.FromHours(2);

    public VersionComparisonService(
        ApplicationDbContext context,
        IAIService aiService,
        ITextExtractionService textExtractionService,
        IAIUsageTrackingService usageTrackingService,
        ILogger<VersionComparisonService> logger,
        IMemoryCache memoryCache)
    {
        _context = context;
        _aiService = aiService;
        _textExtractionService = textExtractionService;
        _usageTrackingService = usageTrackingService;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// Compare two document versions and generate AI analysis
    /// </summary>
    public async Task<ServiceResult<VersionComparison>> CompareVersionsAsync(int fromVersionId, int toVersionId)
    {
        return await CompareVersionsAsync(fromVersionId, toVersionId, AIModel.Gpt4oMini);
    }

    /// <summary>
    /// Compare two document versions and generate AI analysis with specific model
    /// </summary>
    public async Task<ServiceResult<VersionComparison>> CompareVersionsAsync(int fromVersionId, int toVersionId, AIModel model)
    {
        try
        {
            _logger.LogInformation("Comparing versions {FromVersionId} to {ToVersionId}", fromVersionId, toVersionId);

            // Check cache first
            var cacheKey = GetComparisonCacheKey(fromVersionId, toVersionId);
            if (_memoryCache.TryGetValue(cacheKey, out VersionComparison? cachedComparison) && cachedComparison != null)
            {
                _logger.LogDebug("Retrieved version comparison from cache for versions {FromVersionId} to {ToVersionId}", fromVersionId, toVersionId);
                return ServiceResult<VersionComparison>.Success(cachedComparison);
            }

            // Check if comparison already exists in database
            var existingComparison = await _context.VersionComparisons
                .FirstOrDefaultAsync(vc => vc.FromVersionId == fromVersionId && vc.ToVersionId == toVersionId);

            if (existingComparison != null)
            {
                // Cache the database result
                _memoryCache.Set(cacheKey, existingComparison, ComparisonCacheExpiration);
                _logger.LogDebug("Retrieved existing version comparison from database and cached it");
                return ServiceResult<VersionComparison>.Success(existingComparison);
            }

            // Get version details
            var fromVersion = await GetVersionWithCaching(fromVersionId);
            var toVersion = await GetVersionWithCaching(toVersionId);

            if (fromVersion == null || toVersion == null)
            {
                return ServiceResult<VersionComparison>.Failure("One or both versions not found");
            }

            if (fromVersion.DocumentId != toVersion.DocumentId)
            {
                return ServiceResult<VersionComparison>.Failure("Versions belong to different documents");
            }

            // Extract content with caching
            var fromContentResult = await GetVersionContentWithCaching(fromVersionId);
            var toContentResult = await GetVersionContentWithCaching(toVersionId);

            if (!fromContentResult.Succeeded || !toContentResult.Succeeded)
            {
                return ServiceResult<VersionComparison>.Failure("Failed to extract content from one or both versions");
            }

            var fromContent = fromContentResult.Data;
            var toContent = toContentResult.Data;

            // Generate AI comparison
            var aiComparisonResult = await GenerateAIComparison(fromContent, toContent, fromVersion.VersionNumber, toVersion.VersionNumber, model, null, fromVersion.DocumentId);
            if (!aiComparisonResult.Succeeded)
            {
                return ServiceResult<VersionComparison>.Failure(aiComparisonResult.Error!);
            }

            var aiComparison = aiComparisonResult.Data;

            // Create and save comparison
            var comparison = new VersionComparison
            {
                DocumentId = fromVersion.DocumentId,
                FromVersionId = fromVersionId,
                ToVersionId = toVersionId,
                ChangeSummary = aiComparison.ChangeSummary,
                DetailedChanges = aiComparison.DetailedChanges,
                ChangeType = aiComparison.ChangeType,
                Significance = aiComparison.ChangeSignificance,
                ConfidenceScore = aiComparison.ConfidenceScore,
                ProcessingTimeMs = aiComparison.ProcessingTimeMs,
                TokensUsed = aiComparison.TokensUsed,
                AIModel = aiComparison.AIModel,
                GeneratedAt = DateTime.UtcNow
            };

            _context.VersionComparisons.Add(comparison);
            await _context.SaveChangesAsync();

            // Cache the result
            _memoryCache.Set(cacheKey, comparison, ComparisonCacheExpiration);

            _logger.LogInformation("Successfully generated and cached version comparison for versions {FromVersionId} to {ToVersionId}", fromVersionId, toVersionId);
            return ServiceResult<VersionComparison>.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing versions {FromVersionId} to {ToVersionId}", fromVersionId, toVersionId);
            return ServiceResult<VersionComparison>.Failure("Failed to compare versions");
        }
    }

    /// <summary>
    /// Compare two document versions by extracting content and generating AI analysis
    /// </summary>
    public async Task<ServiceResult<VersionComparison>> CompareDocumentVersionsAsync(int documentId, int fromVersionNumber, int toVersionNumber)
    {
        return await CompareDocumentVersionsAsync(documentId, fromVersionNumber, toVersionNumber, AIModel.Gpt4oMini);
    }

    /// <summary>
    /// Compare two document versions by extracting content and generating AI analysis with specific model
    /// </summary>
    public async Task<ServiceResult<VersionComparison>> CompareDocumentVersionsAsync(int documentId, int fromVersionNumber, int toVersionNumber, AIModel model)
    {
        try
        {
            // Get version IDs
            var versions = await _context.DocumentVersions
                .Where(dv => dv.DocumentId == documentId && 
                           (dv.VersionNumber == fromVersionNumber || dv.VersionNumber == toVersionNumber))
                .ToListAsync();

            var fromVersion = versions.FirstOrDefault(v => v.VersionNumber == fromVersionNumber);
            var toVersion = versions.FirstOrDefault(v => v.VersionNumber == toVersionNumber);

            if (fromVersion == null || toVersion == null)
            {
                return ServiceResult<VersionComparison>.Failure("One or both version numbers not found for this document");
            }

            return await CompareVersionsAsync(fromVersion.Id, toVersion.Id, model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing document {DocumentId} versions {FromVersion} to {ToVersion} with model {Model}", 
                documentId, fromVersionNumber, toVersionNumber, model.ToDisplayName());
            return ServiceResult<VersionComparison>.Failure("Failed to compare document versions");
        }
    }

    /// <summary>
    /// Compare two document versions by ID and generate AI analysis with usage tracking
    /// </summary>
    public async Task<ServiceResult<VersionComparison>> CompareVersionsAsync(int fromVersionId, int toVersionId, string userId)
    {
        return await CompareVersionsAsync(fromVersionId, toVersionId, AIModel.Gpt4oMini, userId);
    }

    /// <summary>
    /// Compare two document versions by ID and generate AI analysis with specific model and usage tracking
    /// </summary>
    public async Task<ServiceResult<VersionComparison>> CompareVersionsAsync(int fromVersionId, int toVersionId, AIModel model, string userId)
    {
        try
        {
            _logger.LogInformation("Comparing versions {FromVersionId} to {ToVersionId} for user {UserId}", fromVersionId, toVersionId, userId);

            // Check cache first
            var cacheKey = GetComparisonCacheKey(fromVersionId, toVersionId);
            if (_memoryCache.TryGetValue(cacheKey, out VersionComparison? cachedComparison) && cachedComparison != null)
            {
                _logger.LogDebug("Retrieved version comparison from cache for versions {FromVersionId} to {ToVersionId}", fromVersionId, toVersionId);
                return ServiceResult<VersionComparison>.Success(cachedComparison);
            }

            // Check if comparison already exists in database
            var existingComparison = await _context.VersionComparisons
                .FirstOrDefaultAsync(vc => vc.FromVersionId == fromVersionId && vc.ToVersionId == toVersionId);

            if (existingComparison != null)
            {
                // Cache the database result
                _memoryCache.Set(cacheKey, existingComparison, ComparisonCacheExpiration);
                _logger.LogDebug("Retrieved existing version comparison from database and cached it");
                return ServiceResult<VersionComparison>.Success(existingComparison);
            }

            // Get version details
            var fromVersion = await GetVersionWithCaching(fromVersionId);
            var toVersion = await GetVersionWithCaching(toVersionId);

            if (fromVersion == null || toVersion == null)
            {
                return ServiceResult<VersionComparison>.Failure("One or both versions not found");
            }

            if (fromVersion.DocumentId != toVersion.DocumentId)
            {
                return ServiceResult<VersionComparison>.Failure("Versions belong to different documents");
            }

            // Extract content with caching
            var fromContentResult = await GetVersionContentWithCaching(fromVersionId);
            var toContentResult = await GetVersionContentWithCaching(toVersionId);

            if (!fromContentResult.Succeeded || !toContentResult.Succeeded)
            {
                return ServiceResult<VersionComparison>.Failure("Failed to extract content from one or both versions");
            }

            var fromContent = fromContentResult.Data;
            var toContent = toContentResult.Data;

            // Generate AI comparison WITH userId
            var aiComparisonResult = await GenerateAIComparison(fromContent, toContent, fromVersion.VersionNumber, toVersion.VersionNumber, model, userId, fromVersion.DocumentId);
            if (!aiComparisonResult.Succeeded)
            {
                return ServiceResult<VersionComparison>.Failure(aiComparisonResult.Error!);
            }

            var aiComparison = aiComparisonResult.Data;

            // Create and save comparison
            var comparison = new VersionComparison
            {
                DocumentId = fromVersion.DocumentId,
                FromVersionId = fromVersionId,
                ToVersionId = toVersionId,
                ChangeSummary = aiComparison.ChangeSummary,
                DetailedChanges = aiComparison.DetailedChanges,
                ChangeType = aiComparison.ChangeType,
                Significance = aiComparison.ChangeSignificance,
                ConfidenceScore = aiComparison.ConfidenceScore,
                ProcessingTimeMs = aiComparison.ProcessingTimeMs,
                TokensUsed = aiComparison.TokensUsed,
                AIModel = aiComparison.AIModel,
                GeneratedAt = DateTime.UtcNow
            };

            _context.VersionComparisons.Add(comparison);
            await _context.SaveChangesAsync();

            // Cache the result
            _memoryCache.Set(cacheKey, comparison, ComparisonCacheExpiration);

            _logger.LogInformation("Successfully generated and cached version comparison for versions {FromVersionId} to {ToVersionId}", fromVersionId, toVersionId);
            return ServiceResult<VersionComparison>.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing versions {FromVersionId} to {ToVersionId} for user {UserId}", fromVersionId, toVersionId, userId);
            return ServiceResult<VersionComparison>.Failure("Failed to compare versions");
        }
    }

    /// <summary>
    /// Compare two document versions by version numbers and generate AI analysis with usage tracking
    /// </summary>
    public async Task<ServiceResult<VersionComparison>> CompareDocumentVersionsAsync(int documentId, int fromVersionNumber, int toVersionNumber, string userId)
    {
        return await CompareDocumentVersionsAsync(documentId, fromVersionNumber, toVersionNumber, AIModel.Gpt4oMini, userId);
    }

    /// <summary>
    /// Compare two document versions by version numbers and generate AI analysis with specific model and usage tracking
    /// </summary>
    public async Task<ServiceResult<VersionComparison>> CompareDocumentVersionsAsync(int documentId, int fromVersionNumber, int toVersionNumber, AIModel model, string userId)
    {
        try
        {
            // Get version IDs
            var versions = await _context.DocumentVersions
                .Where(dv => dv.DocumentId == documentId && 
                           (dv.VersionNumber == fromVersionNumber || dv.VersionNumber == toVersionNumber))
                .ToListAsync();

            var fromVersion = versions.FirstOrDefault(v => v.VersionNumber == fromVersionNumber);
            var toVersion = versions.FirstOrDefault(v => v.VersionNumber == toVersionNumber);

            if (fromVersion == null || toVersion == null)
            {
                return ServiceResult<VersionComparison>.Failure("One or both version numbers not found for this document");
            }

            return await CompareVersionsAsync(fromVersion.Id, toVersion.Id, model, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing document {DocumentId} versions {FromVersion} to {ToVersion} with model {Model} for user {UserId}", 
                documentId, fromVersionNumber, toVersionNumber, model.ToDisplayName(), userId);
            return ServiceResult<VersionComparison>.Failure("Failed to compare document versions");
        }
    }

    /// <summary>
    /// Get existing comparison between two document versions
    /// </summary>
    public async Task<VersionComparison?> GetComparisonAsync(int fromVersionId, int toVersionId)
    {
        try
        {
            _logger.LogInformation("Getting version comparison between versions {FromVersionId} and {ToVersionId}", 
                fromVersionId, toVersionId);

            var comparison = await _context.VersionComparisons
                .FirstOrDefaultAsync(vc => vc.FromVersionId == fromVersionId && vc.ToVersionId == toVersionId);

            if (comparison == null)
            {
                _logger.LogInformation("No comparison found between versions {FromVersionId} and {ToVersionId}", 
                    fromVersionId, toVersionId);
                return null;
            }

            _logger.LogInformation("Found comparison between versions {FromVersionId} and {ToVersionId}", 
                fromVersionId, toVersionId);
            return comparison;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version comparison between versions {FromVersionId} and {ToVersionId}", 
                fromVersionId, toVersionId);
            throw;
        }
    }

    /// <summary>
    /// Get version details with caching
    /// </summary>
    private async Task<dynamic?> GetVersionWithCaching(int versionId)
    {
        var cacheKey = $"version_basic_{versionId}";
        
        if (_memoryCache.TryGetValue(cacheKey, out dynamic? cachedVersion) && cachedVersion != null)
        {
            return cachedVersion;
        }

        var version = await _context.DocumentVersions
            .Where(dv => dv.Id == versionId)
            .Select(dv => new { dv.Id, dv.DocumentId, dv.VersionNumber, dv.StoragePath, dv.FileType, dv.CreatedAt })
            .FirstOrDefaultAsync();

        if (version != null)
        {
            _memoryCache.Set(cacheKey, version, TimeSpan.FromHours(1));
        }

        return version;
    }

    /// <summary>
    /// Get version content with caching
    /// </summary>
    private async Task<ServiceResult<string>> GetVersionContentWithCaching(int versionId)
    {
        var cacheKey = $"version_content_{versionId}";
        
        if (_memoryCache.TryGetValue(cacheKey, out string? cachedContent) && !string.IsNullOrEmpty(cachedContent))
        {
            _logger.LogDebug("Retrieved version content from cache for version {VersionId}", versionId);
            return ServiceResult<string>.Success(cachedContent);
        }

        var version = await _context.DocumentVersions.FindAsync(versionId);
        if (version == null)
        {
            return ServiceResult<string>.Failure("Version not found");
        }

        var contentResult = await _textExtractionService.ExtractTextFromDocumentAsync(version.DocumentId, version.VersionNumber);
        
        if (contentResult.Succeeded && !string.IsNullOrWhiteSpace(contentResult.Data))
        {
            // Cache with content hash for invalidation
            var contentHash = ComputeContentHash(contentResult.Data);
            var cacheKeyWithHash = $"{cacheKey}_{contentHash}";
            
            _memoryCache.Set(cacheKeyWithHash, contentResult.Data, ContentCacheExpiration);
            _memoryCache.Set(cacheKey, contentResult.Data, ContentCacheExpiration);
            
            _logger.LogDebug("Cached version content for version {VersionId}", versionId);
        }

        return contentResult;
    }

    /// <summary>
    /// Generate AI comparison
    /// </summary>
    private async Task<ServiceResult<AIComparisonResult>> GenerateAIComparison(string fromContent, string toContent, int fromVersionNumber, int toVersionNumber, AIModel model, string? userId = null, int? documentId = null)
    {
        var startTime = DateTime.UtcNow;
        var servedFromCache = false;
        
        try
        {
            // Create content-based cache key for AI comparison
            var contentKey = ComputeComparisonContentHash(fromContent, toContent);
            var aiCacheKey = $"ai_comparison_{contentKey}";
            
            if (_memoryCache.TryGetValue(aiCacheKey, out AIComparisonResult? cachedAIResult) && cachedAIResult != null)
            {
                _logger.LogDebug("Retrieved AI comparison from cache");
                servedFromCache = true;
                
                // Log cache hit usage if user provided
                if (!string.IsNullOrEmpty(userId))
                {
                    var cacheAIResponse = new AIResponse
                    {
                        IsSuccess = true,
                        Content = cachedAIResult.ChangeSummary + "\n" + cachedAIResult.DetailedChanges,
                        TokensUsed = cachedAIResult.TokensUsed ?? 0,
                        ResponseTime = TimeSpan.FromMilliseconds(50) // Cache response time
                    };
                    
                    await LogUsageAsync(userId, "VersionComparison", cacheAIResponse, documentId, fromContent.Length + toContent.Length, servedFromCache);
                }
                
                return ServiceResult<AIComparisonResult>.Success(cachedAIResult);
            }

            var prompt = $@"Please analyze the differences between two document versions and provide a detailed comparison.

VERSION {fromVersionNumber} (ORIGINAL):
{TruncateContent(fromContent, 2000)}

VERSION {toVersionNumber} (UPDATED):
{TruncateContent(toContent, 2000)}

Please provide:
1. A concise summary of what changed (1-2 sentences)
2. Detailed changes in bullet points
3. Overall change significance (Minor, Medium, High, Critical)

Response format:
SUMMARY: [brief summary]
CHANGES:
• [change 1]
• [change 2]
• [change 3]
SIGNIFICANCE: [Minor|Medium|High|Critical]";

            var aiResponse = await _aiService.GenerateCompletionAsync(prompt, model.ToApiString());
            var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            if (!aiResponse.IsSuccess || string.IsNullOrWhiteSpace(aiResponse.Content))
            {
                // Log failed usage if user provided
                if (!string.IsNullOrEmpty(userId))
                {
                    var failedResponse = new AIResponse
                    {
                        IsSuccess = false,
                        Content = string.Empty,
                        TokensUsed = 0,
                        ResponseTime = TimeSpan.FromMilliseconds(processingTime),
                        ErrorMessage = "Failed to generate AI comparison"
                    };
                    
                    await LogUsageAsync(userId, "VersionComparison", failedResponse, documentId, fromContent.Length + toContent.Length, servedFromCache);
                }
                
                return ServiceResult<AIComparisonResult>.Failure("Failed to generate AI comparison");
            }

            // Log successful usage if user provided
            if (!string.IsNullOrEmpty(userId))
            {
                await LogUsageAsync(userId, "VersionComparison", aiResponse, documentId, fromContent.Length + toContent.Length, servedFromCache);
            }

            var parsedResult = ParseAIComparisonResponse(aiResponse.Content, processingTime, model.ToApiString());
            
            // Cache the AI result for longer period since content won't change
            _memoryCache.Set(aiCacheKey, parsedResult, TimeSpan.FromDays(7));
            
            return ServiceResult<AIComparisonResult>.Success(parsedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI comparison");
            return ServiceResult<AIComparisonResult>.Failure("Failed to generate AI comparison");
        }
    }

    /// <summary>
    /// Parse AI comparison response
    /// </summary>
    private static AIComparisonResult ParseAIComparisonResponse(string aiResponse, int processingTimeMs, string modelName)
    {
        var result = new AIComparisonResult
        {
            ChangeSummary = "Analysis completed",
            DetailedChanges = "",
            ChangeType = ChangeType.ContentModification,
            ChangeSignificance = ChangeSignificance.Medium,
            ConfidenceScore = 0.85,
            ProcessingTimeMs = processingTimeMs,
            TokensUsed = aiResponse.Length / 4, // Rough estimate
            AIModel = modelName
        };

        try
        {
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var changes = new List<string>();
            bool inChanges = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                {
                    result.ChangeSummary = trimmedLine.Substring(8).Trim();
                    inChanges = false;
                }
                else if (trimmedLine.StartsWith("CHANGES:", StringComparison.OrdinalIgnoreCase))
                {
                    inChanges = true;
                }
                else if (trimmedLine.StartsWith("SIGNIFICANCE:", StringComparison.OrdinalIgnoreCase))
                {
                    var significance = trimmedLine.Substring(13).Trim();
                    result.ChangeSignificance = significance.ToLowerInvariant() switch
                    {
                        "critical" => ChangeSignificance.Critical,
                        "high" => ChangeSignificance.High,
                        "medium" => ChangeSignificance.Medium,
                        "minor" => ChangeSignificance.Low,
                        _ => ChangeSignificance.Medium
                    };
                    inChanges = false;
                }
                else if (inChanges && (trimmedLine.StartsWith("•") || trimmedLine.StartsWith("-") || trimmedLine.StartsWith("*")))
                {
                    changes.Add(trimmedLine);
                }
            }

            if (changes.Any())
            {
                result.DetailedChanges = string.Join("\n", changes);
            }

            // Determine change type based on content
            if (result.DetailedChanges.Contains("added", StringComparison.OrdinalIgnoreCase))
                result.ChangeType |= ChangeType.ContentAddition;
            if (result.DetailedChanges.Contains("removed", StringComparison.OrdinalIgnoreCase) || 
                result.DetailedChanges.Contains("deleted", StringComparison.OrdinalIgnoreCase))
                result.ChangeType |= ChangeType.ContentDeletion;
            if (result.DetailedChanges.Contains("structure", StringComparison.OrdinalIgnoreCase) || 
                result.DetailedChanges.Contains("format", StringComparison.OrdinalIgnoreCase))
                result.ChangeType |= ChangeType.StructuralChange;
        }
        catch (Exception)
        {
            // Use defaults if parsing fails
        }

        return result;
    }

    /// <summary>
    /// Truncate content for analysis
    /// </summary>
    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            return content;

        return content.Substring(0, maxLength) + "...\n[Content truncated for analysis]";
    }

    /// <summary>
    /// Get comparison cache key
    /// </summary>
    private static string GetComparisonCacheKey(int fromVersionId, int toVersionId) 
        => $"comparison_{Math.Min(fromVersionId, toVersionId)}_{Math.Max(fromVersionId, toVersionId)}";

    /// <summary>
    /// Compute content hash
    /// </summary>
    private static string ComputeContentHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes)[..8];
    }

    /// <summary>
    /// Compute comparison content hash
    /// </summary>
    private static string ComputeComparisonContentHash(string content1, string content2)
    {
        var combined = content1 + "|" + content2;
        return ComputeContentHash(combined);
    }

    /// <summary>
    /// Get document comparisons
    /// </summary>
    public async Task<ServiceResult<IEnumerable<VersionComparison>>> GetDocumentComparisonsAsync(int documentId)
    {
        try
        {
            var cacheKey = $"document_comparisons_{documentId}";
            
            if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<VersionComparison>? cachedComparisons) && cachedComparisons != null)
            {
                _logger.LogDebug("Retrieved document comparisons from cache for document {DocumentId}", documentId);
                return ServiceResult<IEnumerable<VersionComparison>>.Success(cachedComparisons);
            }

            var comparisons = await _context.VersionComparisons
                .Where(vc => vc.DocumentId == documentId)
                .OrderByDescending(vc => vc.GeneratedAt)
                .ToListAsync();

            // Cache for shorter time since this can change more frequently
            _memoryCache.Set(cacheKey, comparisons, TimeSpan.FromHours(6));

            return ServiceResult<IEnumerable<VersionComparison>>.Success(comparisons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comparisons for document {DocumentId}", documentId);
            return ServiceResult<IEnumerable<VersionComparison>>.Failure("Failed to retrieve document comparisons");
        }
    }

    /// <summary>
    /// Delete a version comparison
    /// </summary>
    public async Task<ServiceResult> DeleteComparisonAsync(int comparisonId)
    {
        try
        {
            var comparison = await _context.VersionComparisons.FindAsync(comparisonId);
            if (comparison == null)
            {
                return ServiceResult.Failure("Comparison not found");
            }

            var documentId = comparison.DocumentId;
            
            _context.VersionComparisons.Remove(comparison);
            await _context.SaveChangesAsync();

            // Invalidate caches
            InvalidateComparisonCaches(comparison.FromVersionId, comparison.ToVersionId, documentId);

            _logger.LogInformation("Successfully deleted comparison {ComparisonId}", comparisonId);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comparison {ComparisonId}", comparisonId);
            return ServiceResult.Failure("Failed to delete comparison");
        }
    }

    /// <summary>
    /// Invalidate comparison caches
    /// </summary>
    private void InvalidateComparisonCaches(int fromVersionId, int toVersionId, int documentId)
    {
        var comparisonKey = GetComparisonCacheKey(fromVersionId, toVersionId);
        var documentKey = $"document_comparisons_{documentId}";
        
        _memoryCache.Remove(comparisonKey);
        _memoryCache.Remove(documentKey);
        
        _logger.LogDebug("Invalidated comparison caches for versions {FromVersionId} to {ToVersionId}", fromVersionId, toVersionId);
    }

    /// <summary>
    /// Log AI usage data
    /// </summary>
    private async Task LogUsageAsync(string userId, string operationType, AIResponse aiResponse, int? documentId, int inputSize, bool servedFromCache)
    {
        try
        {
            await _usageTrackingService.LogUsageAsync(
                userId: userId,
                operationType: operationType,
                aiResponse: aiResponse,
                documentId: documentId,
                inputSize: inputSize,
                servedFromCache: servedFromCache,
                ipAddress: null, // Not available in service layer
                userAgent: null, // Not available in service layer
                qualitySetting: null,
                metadata: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log AI usage for user {UserId}, operation {OperationType}", userId, operationType);
            // Don't fail the main operation if logging fails
        }
    }

    private class AIComparisonResult
    {
        public string ChangeSummary { get; set; } = string.Empty;
        public string DetailedChanges { get; set; } = string.Empty;
        public ChangeType ChangeType { get; set; }
        public ChangeSignificance ChangeSignificance { get; set; }
        public double? ConfidenceScore { get; set; }
        public int ProcessingTimeMs { get; set; }
        public int? TokensUsed { get; set; }
        public string AIModel { get; set; } = string.Empty;
    }
} 