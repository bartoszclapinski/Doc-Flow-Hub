using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DocFlowHub.Infrastructure.Services.AI;

/// <summary>
/// Service for AI-powered document summarization using OpenAI
/// </summary>
public class DocumentSummaryService : IDocumentSummaryService
{
    private readonly IAIService _aiService;
    private readonly ApplicationDbContext _context;
    private readonly IDocumentStorageService _storageService;
    private readonly ILogger<DocumentSummaryService> _logger;
    private readonly IMemoryCache _cache;

    public DocumentSummaryService(
        IAIService aiService,
        ApplicationDbContext context,
        IDocumentStorageService storageService,
        ILogger<DocumentSummaryService> logger,
        IMemoryCache cache)
    {
        _aiService = aiService;
        _context = context;
        _storageService = storageService;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Generate a summary for document content using AI
    /// </summary>
    public async Task<DocumentSummary> GenerateSummaryAsync(string documentContent, string documentTitle)
    {
        try
        {
            _logger.LogInformation("Generating summary for document: {DocumentTitle}", documentTitle);

            // Create cache key for this content
            var contentHash = GetContentHash(documentContent);
            var cacheKey = $"summary_{contentHash}";

            // Check cache first
            if (_cache.TryGetValue(cacheKey, out DocumentSummary? cachedSummary) && cachedSummary != null)
            {
                _logger.LogInformation("Returning cached summary for document: {DocumentTitle}", documentTitle);
                return cachedSummary;
            }

            // Prepare content for AI processing
            var processedContent = PrepareContentForSummarization(documentContent, documentTitle);
            
            // Create AI prompt for summarization
            var systemMessage = "You are a professional document analyst. Create a clear, concise summary of the document content. Focus on key points, main topics, and important information. Keep the summary between 100-300 words.";
            var prompt = $"Document Title: {documentTitle}\n\nContent:\n{processedContent}\n\nPlease provide:\n1. A brief summary (2-3 sentences)\n2. Key points (bullet format)\n\nFormat your response as:\nSUMMARY: [your summary here]\nKEY POINTS:\n• [point 1]\n• [point 2]\n• [point 3]";

            // Call AI service - explicitly use the overload with system message
            var aiResponse = await _aiService.GenerateCompletionAsync(prompt, systemMessage, model: null);

            if (!aiResponse.IsSuccess)
            {
                _logger.LogError("AI service failed to generate summary: {Error}", aiResponse.ErrorMessage);
                throw new InvalidOperationException($"Failed to generate summary: {aiResponse.ErrorMessage}");
            }

            // Parse AI response
            var summary = ParseAIResponse(aiResponse.Content, documentTitle);
            
            // Cache the result for 1 hour
            _cache.Set(cacheKey, summary, TimeSpan.FromHours(1));

            _logger.LogInformation("Successfully generated summary for document: {DocumentTitle}", documentTitle);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for document: {DocumentTitle}", documentTitle);
            throw;
        }
    }

    /// <summary>
    /// Get existing summary for a document from database
    /// </summary>
    public async Task<DocumentSummary?> GetSummaryAsync(int documentId)
    {
        try
        {
            _logger.LogInformation("Getting summary for document ID: {DocumentId}", documentId);
            
            var summary = await _context.DocumentSummaries
                .FirstOrDefaultAsync(s => s.DocumentId == documentId);
            
            if (summary == null)
            {
                _logger.LogInformation("No summary found for document ID: {DocumentId}", documentId);
                return null;
            }

            _logger.LogInformation("Found summary for document ID: {DocumentId}", documentId);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting summary for document ID: {DocumentId}", documentId);
            throw;
        }
    }

    /// <summary>
    /// Regenerate summary for a document by fetching content and storing in database
    /// </summary>
    public async Task<DocumentSummary> RegenerateSummaryAsync(int documentId)
    {
        try
        {
            _logger.LogInformation("Regenerating summary for document ID: {DocumentId}", documentId);
            
            // 1. Fetch document from database
            var document = await _context.Documents
                .Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                throw new ArgumentException($"Document with ID {documentId} not found");
            }

            // 2. Get the latest version
            var latestVersion = document.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            if (latestVersion == null)
            {
                throw new InvalidOperationException($"No versions found for document {documentId}");
            }

            // 3. Download document content
            var contentResult = await _storageService.DownloadDocumentAsync(documentId, latestVersion.VersionNumber);
            if (!contentResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to download document content: {contentResult.Error}");
            }

            // 4. Convert content to text (basic implementation for text files)
            var documentContent = System.Text.Encoding.UTF8.GetString(contentResult.Data ?? Array.Empty<byte>());
            
            // 5. Generate summary
            var summary = await GenerateSummaryAsync(documentContent, document.Title);
            
            // 6. Store in database
            var existingSummary = await _context.DocumentSummaries
                .FirstOrDefaultAsync(s => s.DocumentId == documentId);

            if (existingSummary != null)
            {
                // Update existing summary
                existingSummary.Summary = summary.Summary;
                existingSummary.KeyPoints = summary.KeyPoints;
                existingSummary.GeneratedAt = summary.GeneratedAt;
                existingSummary.AIModel = summary.AIModel;
                existingSummary.ConfidenceScore = summary.ConfidenceScore;
                _context.DocumentSummaries.Update(existingSummary);
            }
            else
            {
                // Create new summary
                summary.DocumentId = documentId;
                _context.DocumentSummaries.Add(summary);
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully regenerated summary for document ID: {DocumentId}", documentId);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating summary for document ID: {DocumentId}", documentId);
            throw;
        }
    }

    private static string PrepareContentForSummarization(string content, string title)
    {
        // Clean and prepare content for AI processing
        var builder = new StringBuilder();
        
        // Split content into lines and process
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines.Take(100)) // Limit to first 100 lines to avoid token limits
        {
            var cleanLine = line.Trim();
            if (!string.IsNullOrEmpty(cleanLine) && cleanLine.Length > 10) // Skip very short lines
            {
                builder.AppendLine(cleanLine);
            }
        }

        var processedContent = builder.ToString();
        
        // Ensure content is not too long (limit to ~3000 characters to leave room for prompt)
        if (processedContent.Length > 3000)
        {
            processedContent = processedContent.Substring(0, 3000) + "...";
        }

        return processedContent;
    }

    private static DocumentSummary ParseAIResponse(string aiResponse, string documentTitle)
    {
        var summary = new DocumentSummary
        {
            GeneratedAt = DateTime.UtcNow,
            AIModel = "gpt-4o-mini", // From configuration
            ConfidenceScore = 0.8 // Default confidence score
        };

        try
        {
            // Parse the structured response
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var summaryBuilder = new StringBuilder();
            var keyPointsBuilder = new StringBuilder();
            bool inKeyPoints = false;

            foreach (var line in lines)
            {
                var cleanLine = line.Trim();
                
                if (cleanLine.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                {
                    summaryBuilder.AppendLine(cleanLine.Substring(8).Trim());
                    inKeyPoints = false;
                }
                else if (cleanLine.StartsWith("KEY POINTS:", StringComparison.OrdinalIgnoreCase))
                {
                    inKeyPoints = true;
                }
                else if (inKeyPoints && cleanLine.StartsWith("•"))
                {
                    keyPointsBuilder.AppendLine(cleanLine);
                }
                else if (!inKeyPoints && !cleanLine.StartsWith("SUMMARY:"))
                {
                    summaryBuilder.AppendLine(cleanLine);
                }
            }

            summary.Summary = summaryBuilder.ToString().Trim();
            summary.KeyPoints = keyPointsBuilder.ToString().Trim();

            // Fallback if parsing fails
            if (string.IsNullOrEmpty(summary.Summary))
            {
                summary.Summary = aiResponse.Length > 500 
                    ? aiResponse.Substring(0, 500) + "..."
                    : aiResponse;
            }
        }
        catch (Exception)
        {
            // Fallback to raw response if parsing fails
            summary.Summary = aiResponse.Length > 500 
                ? aiResponse.Substring(0, 500) + "..."
                : aiResponse;
            summary.KeyPoints = null;
        }

        return summary;
    }

    private static string GetContentHash(string content)
    {
        // Simple hash for caching purposes
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(bytes)[..16]; // Take first 16 characters
    }
} 