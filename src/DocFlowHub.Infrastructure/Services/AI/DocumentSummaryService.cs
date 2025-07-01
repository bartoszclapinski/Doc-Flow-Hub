using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace DocFlowHub.Infrastructure.Services.AI;

/// <summary>
/// Service for AI-powered document summarization using OpenAI
/// </summary>
public class DocumentSummaryService : IDocumentSummaryService
{
    private readonly ApplicationDbContext _context;
    private readonly IAIService _aiService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly IAIUsageTrackingService _usageTrackingService;
    private readonly ILogger<DocumentSummaryService> _logger;
    private readonly IMemoryCache _memoryCache;
    
    // Cache keys and expiration times
    private static readonly TimeSpan SummaryCacheExpiration = TimeSpan.FromHours(24);
    private static readonly TimeSpan DocumentContentCacheExpiration = TimeSpan.FromHours(1);

    public DocumentSummaryService(
        ApplicationDbContext context,
        IAIService aiService,
        ITextExtractionService textExtractionService,
        IAIUsageTrackingService usageTrackingService,
        ILogger<DocumentSummaryService> logger,
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
    /// Get existing summary for a document from database
    /// </summary>
    public async Task<ServiceResult<DocumentSummary>> GetSummaryAsync(int documentId)
    {
        try
        {
            // Check cache first
            var cacheKey = GetSummaryCacheKey(documentId);
            if (_memoryCache.TryGetValue(cacheKey, out DocumentSummary? cachedSummary) && cachedSummary != null)
            {
                _logger.LogDebug("Retrieved document summary from cache for document ID: {DocumentId}", documentId);
                return ServiceResult<DocumentSummary>.Success(cachedSummary);
            }

            var summary = await _context.DocumentSummaries
                .FirstOrDefaultAsync(ds => ds.DocumentId == documentId);

            if (summary == null)
            {
                return ServiceResult<DocumentSummary>.Failure("Summary not found");
            }

            // Cache the result
            _memoryCache.Set(cacheKey, summary, SummaryCacheExpiration);
            _logger.LogDebug("Cached document summary for document ID: {DocumentId}", documentId);

            return ServiceResult<DocumentSummary>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document summary for document ID: {DocumentId}", documentId);
            return ServiceResult<DocumentSummary>.Failure("Failed to retrieve document summary");
        }
    }

    /// <summary>
    /// Generate a summary for document content using AI
    /// </summary>
    public async Task<ServiceResult<DocumentSummary>> GenerateSummaryAsync(int documentId)
    {
        return await GenerateSummaryAsync(documentId, AIModel.Gpt4oMini);
    }

    /// <summary>
    /// Generate a summary for document content using AI with specific model
    /// </summary>
    public async Task<ServiceResult<DocumentSummary>> GenerateSummaryAsync(int documentId, AIModel model)
    {
        try
        {
            _logger.LogInformation("Generating summary for document ID: {DocumentId}", documentId);
            
            // Get document details with caching
            var document = await GetDocumentWithCaching(documentId);
            if (document == null)
            {
                return ServiceResult<DocumentSummary>.Failure("Document not found");
            }

            _logger.LogInformation("Generating summary for document: {DocumentTitle}", (string)document.Title);

            // Extract document content with caching
            var contentResult = await GetDocumentContentWithCaching(documentId);
            if (!contentResult.Succeeded || string.IsNullOrWhiteSpace(contentResult.Data))
            {
                return ServiceResult<DocumentSummary>.Failure("Failed to extract document content for summary generation");
            }

            var content = contentResult.Data;
            _logger.LogDebug("Extracted content length: {ContentLength} characters", content.Length);

            // Generate AI summary
            var prompt = $@"Please provide a comprehensive summary of the following document.

Document Title: {document.Title}
Content:
{content}

Please provide:
1. A concise summary (2-3 sentences)
2. Key points (bullet format, 3-5 main points)

Summary:";

            var aiResponse = await _aiService.GenerateCompletionAsync(prompt, model.ToApiString());
            if (!aiResponse.IsSuccess || string.IsNullOrWhiteSpace(aiResponse.Content))
            {
                return ServiceResult<DocumentSummary>.Failure("Failed to generate AI summary");
            }

            // Log usage if we have a way to get the user (for now, skip if no userId available)
            // Note: This service is often called in background processes without a user context

            // Parse AI response
            var aiContent = aiResponse.Content;
            var (summary, keyPoints) = ParseAISummaryResponse(aiContent);

            // Create and save summary
            var documentSummary = new DocumentSummary
            {
                DocumentId = documentId,
                Summary = summary,
                KeyPoints = keyPoints,
                AIModel = model.ToApiString(),
                ConfidenceScore = 0.85, // Could be enhanced based on AI response
                GeneratedAt = DateTime.UtcNow
            };

            _context.DocumentSummaries.Add(documentSummary);
            await _context.SaveChangesAsync();

            // Invalidate and update cache
            InvalidateSummaryCache(documentId);
            var cacheKey = GetSummaryCacheKey(documentId);
            _memoryCache.Set(cacheKey, documentSummary, SummaryCacheExpiration);

            _logger.LogInformation("Successfully generated and cached summary for document: {DocumentTitle}", (string)document.Title);
            return ServiceResult<DocumentSummary>.Success(documentSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for document ID: {DocumentId}", documentId);
            return ServiceResult<DocumentSummary>.Failure("Failed to generate document summary");
        }
    }

    /// <summary>
    /// Regenerate summary for a document by fetching content and storing in database
    /// </summary>
    public async Task<ServiceResult> RegenerateSummaryAsync(int documentId)
    {
        return await RegenerateSummaryAsync(documentId, AIModel.Gpt4oMini);
    }

    /// <summary>
    /// Regenerate summary for a document with specific model
    /// </summary>
    public async Task<ServiceResult> RegenerateSummaryAsync(int documentId, AIModel model)
    {
        try
        {
            _logger.LogInformation("Regenerating summary for document ID: {DocumentId} with model: {Model}", documentId, model.ToDisplayName());
            
            // Delete existing summary
            var existingSummary = await _context.DocumentSummaries
                .FirstOrDefaultAsync(ds => ds.DocumentId == documentId);

            if (existingSummary != null)
            {
                _context.DocumentSummaries.Remove(existingSummary);
                await _context.SaveChangesAsync();
                _logger.LogDebug("Removed existing summary for document ID: {DocumentId}", documentId);
            }

            // Invalidate caches
            InvalidateSummaryCache(documentId);
            InvalidateDocumentContentCache(documentId);

            // Generate new summary with specified model
            var result = await GenerateSummaryAsync(documentId, model);
            if (!result.Succeeded)
            {
                return ServiceResult.Failure(result.Error!);
            }

            _logger.LogInformation("Successfully regenerated summary for document ID: {DocumentId} with model: {Model}", documentId, model.ToDisplayName());
            return ServiceResult.Success();
            }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating summary for document ID: {DocumentId} with model: {Model}", documentId, model.ToDisplayName());
            return ServiceResult.Failure("Failed to regenerate document summary");
        }
    }

    public async Task<ServiceResult> DeleteSummaryAsync(int documentId)
    {
        try
        {
            var summary = await _context.DocumentSummaries
                .FirstOrDefaultAsync(ds => ds.DocumentId == documentId);

            if (summary == null)
            {
                return ServiceResult.Failure("Summary not found");
            }

            _context.DocumentSummaries.Remove(summary);
            await _context.SaveChangesAsync();
            
            // Invalidate cache
            InvalidateSummaryCache(documentId);

            _logger.LogInformation("Successfully deleted summary for document ID: {DocumentId}", documentId);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting summary for document ID: {DocumentId}", documentId);
            return ServiceResult.Failure("Failed to delete document summary");
        }
    }

    #region Private Methods

    private async Task<dynamic?> GetDocumentWithCaching(int documentId)
    {
        var cacheKey = $"document_basic_{documentId}";
        
        if (_memoryCache.TryGetValue(cacheKey, out dynamic? cachedDocument) && cachedDocument != null)
        {
            return cachedDocument;
        }

        var document = await _context.Documents
            .Where(d => d.Id == documentId && !d.IsDeleted)
            .Select(d => new { d.Id, d.Title, d.FilePath, d.FileType })
            .FirstOrDefaultAsync();

        if (document != null)
        {
            _memoryCache.Set(cacheKey, document, TimeSpan.FromMinutes(30));
        }

        return document;
    }

    private async Task<ServiceResult<string>> GetDocumentContentWithCaching(int documentId)
        {
        var cacheKey = GetDocumentContentCacheKey(documentId);
        
        if (_memoryCache.TryGetValue(cacheKey, out string? cachedContent) && !string.IsNullOrEmpty(cachedContent))
        {
            _logger.LogDebug("Retrieved document content from cache for document ID: {DocumentId}", documentId);
            return ServiceResult<string>.Success(cachedContent);
            }

        // Get latest version
        var latestVersion = await _context.DocumentVersions
            .Where(dv => dv.DocumentId == documentId)
            .OrderByDescending(dv => dv.VersionNumber)
            .FirstOrDefaultAsync();

        if (latestVersion == null)
        {
            return ServiceResult<string>.Failure("No document versions found");
        }

        // Extract content using text extraction service
        var contentResult = await _textExtractionService.ExtractTextFromDocumentAsync(latestVersion.DocumentId, latestVersion.VersionNumber);
        
        if (contentResult.Succeeded && !string.IsNullOrWhiteSpace(contentResult.Data))
        {
            // Cache the extracted content with a hash for content-based invalidation
            var contentHash = ComputeContentHash(contentResult.Data);
            var cacheKeyWithHash = $"{cacheKey}_{contentHash}";
            
            _memoryCache.Set(cacheKeyWithHash, contentResult.Data, DocumentContentCacheExpiration);
            _memoryCache.Set(cacheKey, contentResult.Data, DocumentContentCacheExpiration);
            
            _logger.LogDebug("Cached document content for document ID: {DocumentId}", documentId);
        }

        return contentResult;
    }

    private static (string summary, string keyPoints) ParseAISummaryResponse(string aiResponse)
    {
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var summary = "";
        var keyPoints = "";
        var inKeyPoints = false;

            foreach (var line in lines)
            {
            var trimmedLine = line.Trim();
                
            if (trimmedLine.StartsWith("Key points", StringComparison.OrdinalIgnoreCase) || 
                trimmedLine.StartsWith("Key Points", StringComparison.OrdinalIgnoreCase))
                {
                    inKeyPoints = true;
                continue;
            }

            if (inKeyPoints)
            {
                if (trimmedLine.StartsWith("•") || trimmedLine.StartsWith("-") || trimmedLine.StartsWith("*"))
                {
                    keyPoints += trimmedLine + "\n";
                }
                }
            else if (!string.IsNullOrWhiteSpace(trimmedLine) && 
                     !trimmedLine.StartsWith("Summary", StringComparison.OrdinalIgnoreCase))
                {
                summary += trimmedLine + " ";
                }
            }

        return (summary.Trim(), keyPoints.Trim());
    }

    private static string GetSummaryCacheKey(int documentId) => $"document_summary_{documentId}";
    private static string GetDocumentContentCacheKey(int documentId) => $"document_content_{documentId}";

    private void InvalidateSummaryCache(int documentId)
            {
        var cacheKey = GetSummaryCacheKey(documentId);
        _memoryCache.Remove(cacheKey);
        _logger.LogDebug("Invalidated summary cache for document ID: {DocumentId}", documentId);
    }

    private void InvalidateDocumentContentCache(int documentId)
    {
        var cacheKey = GetDocumentContentCacheKey(documentId);
        _memoryCache.Remove(cacheKey);
        _logger.LogDebug("Invalidated content cache for document ID: {DocumentId}", documentId);
    }

    private static string ComputeContentHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes)[..8]; // Short hash for cache key
    }

    /// <summary>
    /// Log AI usage data (if userId available)
    /// </summary>
    private async Task LogUsageAsync(string? userId, string operationType, AIResponse aiResponse, int? documentId, int inputSize, bool servedFromCache)
    {
        if (string.IsNullOrEmpty(userId))
        {
            // Skip logging if no userId (e.g., background processes)
            return;
        }

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

    #endregion
} 