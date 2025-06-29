using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Services.AI;
using DocFlowHub.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.AI;

/// <summary>
/// Unit tests for DocumentSummaryService
/// </summary>
public class DocumentSummaryServiceTests
{
    private readonly Mock<IAIService> _mockAIService;
    private readonly Mock<ILogger<DocumentSummaryService>> _mockLogger;
    private readonly Mock<IDocumentStorageService> _mockStorageService;
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly DocumentSummaryService _service;

    public DocumentSummaryServiceTests()
    {
        _mockAIService = new Mock<IAIService>();
        _mockLogger = new Mock<ILogger<DocumentSummaryService>>();
        _mockStorageService = new Mock<IDocumentStorageService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        
        _service = new DocumentSummaryService(
            _mockAIService.Object, 
            _context,
            _mockStorageService.Object,
            _mockLogger.Object, 
            _memoryCache);
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithValidContent_ReturnsSuccessfulSummary()
    {
        // Arrange
        var documentTitle = "Test Document";
        var documentContent = "This is a test document with some meaningful content that should be summarized by the AI service.";
        
        var expectedAIResponse = new AIResponse
        {
            IsSuccess = true,
            Content = "SUMMARY: This is a test document.\nKEY POINTS:\n• Contains meaningful content\n• Should be summarized",
            Model = "gpt-4o-mini",
            TokensUsed = 50
        };

        _mockAIService.Setup(x => x.GenerateCompletionAsync(
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .ReturnsAsync(expectedAIResponse);

        // Act
        var result = await _service.GenerateSummaryAsync(documentContent, documentTitle);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("This is a test document.", result.Summary);
        Assert.Equal("• Contains meaningful content\r\n• Should be summarized", result.KeyPoints);
        Assert.Equal("gpt-4o-mini", result.AIModel);
        Assert.Equal(0.8, result.ConfidenceScore);
        Assert.True(result.GeneratedAt <= DateTime.UtcNow);
        Assert.True(result.GeneratedAt >= DateTime.UtcNow.AddMinutes(-1));

        // Verify AI service was called
        _mockAIService.Verify(x => x.GenerateCompletionAsync(
            It.IsAny<string>(), 
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithCachedContent_ReturnsCachedResult()
    {
        // Arrange
        var documentTitle = "Test Document";
        var documentContent = "This is a test document.";
        
        // First call to populate cache
        var expectedAIResponse = new AIResponse
        {
            IsSuccess = true,
            Content = "SUMMARY: This is a test document.\nKEY POINTS:\n• Test content",
            Model = "gpt-4o-mini",
            TokensUsed = 30
        };

        _mockAIService.Setup(x => x.GenerateCompletionAsync(
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .ReturnsAsync(expectedAIResponse);

        // Act - First call
        var firstResult = await _service.GenerateSummaryAsync(documentContent, documentTitle);
        
        // Act - Second call (should use cache)
        var secondResult = await _service.GenerateSummaryAsync(documentContent, documentTitle);

        // Assert
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.Equal(firstResult.Summary, secondResult.Summary);
        Assert.Equal(firstResult.KeyPoints, secondResult.KeyPoints);

        // Verify AI service was called only once
        _mockAIService.Verify(x => x.GenerateCompletionAsync(
            It.IsAny<string>(), 
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithAIServiceFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var documentTitle = "Test Document";
        var documentContent = "This is a test document.";
        
        var expectedAIResponse = new AIResponse
        {
            IsSuccess = false,
            ErrorMessage = "AI service is temporarily unavailable",
            Content = string.Empty,
            Model = "gpt-4o-mini",
            TokensUsed = 0
        };

        _mockAIService.Setup(x => x.GenerateCompletionAsync(
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .ReturnsAsync(expectedAIResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateSummaryAsync(documentContent, documentTitle));
        
        Assert.Contains("Failed to generate summary", exception.Message);
        Assert.Contains("AI service is temporarily unavailable", exception.Message);
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithLongContent_TruncatesContent()
    {
        // Arrange
        var documentTitle = "Long Document";
        var documentContent = new string('A', 5000); // 5000 characters
        
        var expectedAIResponse = new AIResponse
        {
            IsSuccess = true,
            Content = "SUMMARY: This is a long document.\nKEY POINTS:\n• Very long content",
            Model = "gpt-4o-mini",
            TokensUsed = 40
        };

        string capturedPrompt = string.Empty;
        _mockAIService.Setup(x => x.GenerateCompletionAsync(
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .Callback<string, string>((prompt, system) => capturedPrompt = prompt)
            .ReturnsAsync(expectedAIResponse);

        // Act
        var result = await _service.GenerateSummaryAsync(documentContent, documentTitle);

        // Assert
        Assert.NotNull(result);
        Assert.True(capturedPrompt.Length < documentContent.Length, "Content should be truncated");
        Assert.Contains("...", capturedPrompt); // Should contain truncation indicator
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithMalformedAIResponse_UsesFallbackParsing()
    {
        // Arrange
        var documentTitle = "Test Document";
        var documentContent = "This is a test document.";
        
        var expectedAIResponse = new AIResponse
        {
            IsSuccess = true,
            Content = "This is just plain text without proper formatting",
            Model = "gpt-4o-mini",
            TokensUsed = 25
        };

        _mockAIService.Setup(x => x.GenerateCompletionAsync(
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .ReturnsAsync(expectedAIResponse);

        // Act
        var result = await _service.GenerateSummaryAsync(documentContent, documentTitle);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("This is just plain text without proper formatting", result.Summary);
        Assert.True(string.IsNullOrEmpty(result.KeyPoints)); // Should be empty/null for malformed response
        Assert.Equal("gpt-4o-mini", result.AIModel);
    }

    [Fact]
    public async Task GetSummaryAsync_WithNonExistentDocument_ReturnsNull()
    {
        // Arrange
        var documentId = 123;

        // Act
        var result = await _service.GetSummaryAsync(documentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RegenerateSummaryAsync_WithNonExistentDocument_ThrowsArgumentException()
    {
        // Arrange
        var documentId = 123;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.RegenerateSummaryAsync(documentId));
    }

    [Theory]
    [InlineData("", "Empty Document")]
    [InlineData("   ", "Whitespace Document")]
    [InlineData("Short", "Short Document")]
    public async Task GenerateSummaryAsync_WithVariousContentSizes_HandlesGracefully(string content, string title)
    {
        // Arrange
        var expectedAIResponse = new AIResponse
        {
            IsSuccess = true,
            Content = $"SUMMARY: {title} summary.\nKEY POINTS:\n• Handled gracefully",
            Model = "gpt-4o-mini",
            TokensUsed = 20
        };

        _mockAIService.Setup(x => x.GenerateCompletionAsync(
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .ReturnsAsync(expectedAIResponse);

        // Act
        var result = await _service.GenerateSummaryAsync(content, title);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("summary", result.Summary);
        Assert.Equal("gpt-4o-mini", result.AIModel);
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithMultilineContent_ProcessesCorrectly()
    {
        // Arrange
        var documentTitle = "Multiline Document";
        var documentContent = @"Line 1: Introduction
Line 2: Main content here
Line 3: 

Line 5: Conclusion
Line 6: End of document";
        
        var expectedAIResponse = new AIResponse
        {
            IsSuccess = true,
            Content = "SUMMARY: Multiline document with introduction and conclusion.\nKEY POINTS:\n• Has introduction\n• Has conclusion",
            Model = "gpt-4o-mini",
            TokensUsed = 35
        };

        _mockAIService.Setup(x => x.GenerateCompletionAsync(
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .ReturnsAsync(expectedAIResponse);

        // Act
        var result = await _service.GenerateSummaryAsync(documentContent, documentTitle);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Multiline document", result.Summary);
        Assert.Contains("Has introduction", result.KeyPoints);
        Assert.Contains("Has conclusion", result.KeyPoints);
    }
} 