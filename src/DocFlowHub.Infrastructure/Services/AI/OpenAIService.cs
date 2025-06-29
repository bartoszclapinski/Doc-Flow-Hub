using OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.AI;
using System.Diagnostics;

namespace DocFlowHub.Infrastructure.Services.AI;

/// <summary>
/// OpenAI service implementation for AI integration
/// </summary>
public class OpenAIService : IAIService
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAIService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _defaultModel;

    public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger, IMemoryCache cache)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Please set OpenAI:ApiKey in appsettings.");
        }

        _client = new OpenAIClient(apiKey);
        _logger = logger;
        _cache = cache;
        _defaultModel = configuration["OpenAI:Model"] ?? "gpt-3.5-turbo";
        
        _logger.LogInformation("OpenAI service initialized with default model: {Model}", _defaultModel);
    }

    /// <summary>
    /// Test connectivity to OpenAI API
    /// </summary>
    public async Task<bool> TestConnectivityAsync()
    {
        try
        {
            _logger.LogInformation("Testing OpenAI service connectivity...");
            
            // Simple test prompt to verify API connectivity
            var chatClient = _client.GetChatClient(_defaultModel);
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful assistant. Respond with just 'OK' to test connectivity."),
                new UserChatMessage("Test connection")
            };

            var response = await chatClient.CompleteChatAsync(messages);
            var content = response.Value.Content[0].Text;

            _logger.LogInformation("OpenAI connectivity test successful. Response: {Response}", content);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI service connectivity test failed");
            return false;
        }
    }

    /// <summary>
    /// Get AI service health status
    /// </summary>
    public async Task<AIServiceHealth> GetHealthAsync()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            bool isConnected = await TestConnectivityAsync();
            stopwatch.Stop();

            return new AIServiceHealth
            {
                IsHealthy = isConnected,
                ResponseTime = stopwatch.Elapsed,
                CheckedAt = DateTime.UtcNow,
                Status = isConnected ? "Connected to OpenAI API successfully" : "Failed to connect to OpenAI API"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI service health check failed");
            return new AIServiceHealth
            {
                IsHealthy = false,
                ResponseTime = TimeSpan.Zero,
                CheckedAt = DateTime.UtcNow,
                Status = "Failed to check AI service health",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Generate a completion using the specified model
    /// </summary>
    public async Task<AIResponse> GenerateCompletionAsync(string prompt, string? model = null)
    {
        // Use configured model if none specified
        var selectedModel = model ?? _defaultModel;
        
        try
        {
            _logger.LogInformation("Generating completion for prompt length: {Length} characters using model: {Model}", prompt.Length, selectedModel);
            
            var stopwatch = Stopwatch.StartNew();
            var chatClient = _client.GetChatClient(selectedModel);
            var messages = new List<ChatMessage>
            {
                new UserChatMessage(prompt)
            };

            var response = await chatClient.CompleteChatAsync(messages);
            stopwatch.Stop();
            
            var content = response.Value.Content[0].Text;
            var tokensUsed = response.Value.Usage?.TotalTokenCount ?? 0;

            _logger.LogInformation("Completion generated successfully. Response length: {Length} characters, Tokens used: {Tokens}", content.Length, tokensUsed);

            return new AIResponse
            {
                Content = content,
                Model = selectedModel,
                TokensUsed = tokensUsed,
                ResponseTime = stopwatch.Elapsed,
                IsSuccess = true,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate completion");
            return new AIResponse
            {
                Content = string.Empty,
                Model = selectedModel,
                TokensUsed = 0,
                ResponseTime = TimeSpan.Zero,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Generate a completion with system message and user prompt
    /// </summary>
    public async Task<AIResponse> GenerateCompletionAsync(string prompt, string systemMessage, string? model = null)
    {
        // Use configured model if none specified
        var selectedModel = model ?? _defaultModel;
        
        try
        {
            _logger.LogInformation("Generating completion for prompt length: {Length} characters using model: {Model}", prompt.Length, selectedModel);
            
            var stopwatch = Stopwatch.StartNew();
            var chatClient = _client.GetChatClient(selectedModel);
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemMessage),
                new UserChatMessage(prompt)
            };

            var response = await chatClient.CompleteChatAsync(messages);
            stopwatch.Stop();
            
            var content = response.Value.Content[0].Text;
            var tokensUsed = response.Value.Usage?.TotalTokenCount ?? 0;

            _logger.LogInformation("Completion generated successfully. Response length: {Length} characters, Tokens used: {Tokens}", content.Length, tokensUsed);

            return new AIResponse
            {
                Content = content,
                Model = selectedModel,
                TokensUsed = tokensUsed,
                ResponseTime = stopwatch.Elapsed,
                IsSuccess = true,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate completion");
            return new AIResponse
            {
                Content = string.Empty,
                Model = selectedModel,
                TokensUsed = 0,
                ResponseTime = TimeSpan.Zero,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }
} 