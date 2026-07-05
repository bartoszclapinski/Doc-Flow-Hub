using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.AI;
using System.Diagnostics;

namespace DocFlowHub.Infrastructure.Services.AI;

/// <summary>
/// Anthropic Claude implementation of <see cref="IAIService"/>.
/// The SDK auto-retries transient failures (429/5xx/network) with exponential backoff,
/// so unlike OpenAIService no hand-rolled retry loop is needed here.
/// </summary>
public class ClaudeAIService : IAIService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<ClaudeAIService> _logger;
    private readonly string _defaultModel;
    private readonly long _maxTokens;

    public ClaudeAIService(IConfiguration configuration, ILogger<ClaudeAIService> logger)
    {
        // Prefer the project-namespaced key; fall back to the conventional Anthropic:ApiKey.
        var apiKey = configuration["DocFlowHub:AnthropicApiKey"] ?? configuration["Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "Anthropic API key is not configured. Please set DocFlowHub:AnthropicApiKey (or Anthropic:ApiKey) via user-secrets or appsettings.");
        }

        _client = new AnthropicClient { ApiKey = apiKey };
        _logger = logger;
        _defaultModel = configuration["Anthropic:Model"] ?? AIModel.ClaudeHaiku45.ToApiString();
        _maxTokens = long.TryParse(configuration["Anthropic:MaxTokens"], out var maxTokens) ? maxTokens : 4096;

        _logger.LogInformation("Anthropic Claude service initialized with default model: {Model}", _defaultModel);
    }

    /// <summary>
    /// Test connectivity to the Anthropic API
    /// </summary>
    public async Task<bool> TestConnectivityAsync()
    {
        try
        {
            _logger.LogInformation("Testing Anthropic Claude service connectivity...");

            // Cap output at 1 token: this probe only needs to confirm the API answers, and
            // GetHealthAsync can be polled by dashboards — no reason to pay for a full reply.
            var response = await CreateMessageAsync(
                "Test connection",
                "You are a helpful assistant. Respond with just 'OK' to test connectivity.",
                _defaultModel,
                maxTokens: 1);

            _logger.LogInformation("Anthropic connectivity test successful. Response: {Response}", GetText(response));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic Claude service connectivity test failed");
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
                Status = isConnected ? "Connected to Anthropic API successfully" : "Failed to connect to Anthropic API"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic Claude service health check failed");
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
    public Task<AIResponse> GenerateCompletionAsync(string prompt, string? model = null)
        => GenerateAsync(prompt, systemMessage: null, model);

    /// <summary>
    /// Generate a completion with system message and user prompt
    /// </summary>
    public Task<AIResponse> GenerateCompletionAsync(string prompt, string systemMessage, string? model = null)
        => GenerateAsync(prompt, systemMessage, model);

    private async Task<AIResponse> GenerateAsync(string prompt, string? systemMessage, string? model)
    {
        var selectedModel = ResolveModel(model);

        try
        {
            _logger.LogInformation(
                "Generating Claude completion for prompt length: {Length} characters using model: {Model}",
                prompt.Length, selectedModel);

            var stopwatch = Stopwatch.StartNew();
            var response = await CreateMessageAsync(prompt, systemMessage, selectedModel);
            stopwatch.Stop();

            var content = GetText(response);
            var inputTokens = (int)response.Usage.InputTokens;
            var outputTokens = (int)response.Usage.OutputTokens;

            _logger.LogInformation(
                "Claude completion generated successfully. Response length: {Length} characters, Tokens used: {Tokens} (in: {In}, out: {Out})",
                content.Length, inputTokens + outputTokens, inputTokens, outputTokens);

            return new AIResponse
            {
                Content = content,
                Model = selectedModel,
                TokensUsed = inputTokens + outputTokens,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                ResponseTime = stopwatch.Elapsed,
                IsSuccess = true,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Claude completion");
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

    private async Task<Message> CreateMessageAsync(string prompt, string? systemMessage, string model, long? maxTokens = null)
    {
        var outputCap = maxTokens ?? _maxTokens;

        // Role is fully qualified to avoid collision with DocFlowHub.Infrastructure.Services.Role
        var userMessage = new MessageParam
        {
            Role = Anthropic.Models.Messages.Role.User,
            Content = prompt
        };

        var parameters = string.IsNullOrEmpty(systemMessage)
            ? new MessageCreateParams
            {
                Model = model,
                MaxTokens = outputCap,
                Messages = [userMessage]
            }
            : new MessageCreateParams
            {
                Model = model,
                MaxTokens = outputCap,
                System = systemMessage,
                Messages = [userMessage]
            };

        return await _client.Messages.Create(parameters);
    }

    private static string GetText(Message response)
        => string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));

    /// <summary>
    /// Guard against cross-provider model strings: if a non-Claude model (e.g. a user's
    /// stored GPT preference) reaches this service, fall back to the configured default.
    /// </summary>
    private string ResolveModel(string? model)
    {
        if (string.IsNullOrEmpty(model))
        {
            return _defaultModel;
        }

        if (!model.StartsWith("claude", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Non-Claude model '{Model}' requested from ClaudeAIService; falling back to {Default}",
                model, _defaultModel);
            return _defaultModel;
        }

        return model;
    }
}
