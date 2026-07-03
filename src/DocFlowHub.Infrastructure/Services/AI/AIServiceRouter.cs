using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.AI;

namespace DocFlowHub.Infrastructure.Services.AI;

/// <summary>
/// Routes AI requests to the correct provider (OpenAI or Anthropic) based on the
/// requested model string ("claude-*" → Anthropic, otherwise OpenAI; null → the
/// application default model's provider). Provider services are resolved lazily so
/// a missing API key for a provider the user never selects does not break the app —
/// it only surfaces as a failed AIResponse if a model from that provider is chosen.
/// </summary>
public class AIServiceRouter : IAIService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AIServiceRouter> _logger;

    public AIServiceRouter(IServiceProvider services, ILogger<AIServiceRouter> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task<bool> TestConnectivityAsync()
    {
        var (service, _) = Resolve(null);
        return service is null ? Task.FromResult(false) : service.TestConnectivityAsync();
    }

    public async Task<AIServiceHealth> GetHealthAsync()
    {
        var (service, error) = Resolve(null);
        if (service is null)
        {
            return new AIServiceHealth
            {
                IsHealthy = false,
                ResponseTime = TimeSpan.Zero,
                CheckedAt = DateTime.UtcNow,
                Status = "AI provider is not configured",
                ErrorMessage = error
            };
        }

        return await service.GetHealthAsync();
    }

    public async Task<AIResponse> GenerateCompletionAsync(string prompt, string? model = null)
    {
        var (service, error) = Resolve(model);
        return service is null
            ? Failure(model, error)
            : await service.GenerateCompletionAsync(prompt, model);
    }

    public async Task<AIResponse> GenerateCompletionAsync(string prompt, string systemMessage, string? model = null)
    {
        var (service, error) = Resolve(model);
        return service is null
            ? Failure(model, error)
            : await service.GenerateCompletionAsync(prompt, systemMessage, model);
    }

    /// <summary>
    /// Resolve the provider service for the requested model. Construction can throw
    /// (missing API key) — converted here into a (null, error) result so callers get
    /// a clean failed AIResponse instead of a 500.
    /// </summary>
    private (IAIService? Service, string? Error) Resolve(string? model)
    {
        var provider = AIModelHelper.GetProviderForApiString(model);
        try
        {
            IAIService service = provider == AIProvider.Anthropic
                ? _services.GetRequiredService<ClaudeAIService>()
                : _services.GetRequiredService<OpenAIService>();
            return (service, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve AI provider {Provider} for model '{Model}'", provider, model);
            return (null, $"{provider} is not configured: {ex.Message}");
        }
    }

    private static AIResponse Failure(string? model, string? error) => new()
    {
        Content = string.Empty,
        Model = model ?? AIModelHelper.GetDefaultModel().ToApiString(),
        TokensUsed = 0,
        ResponseTime = TimeSpan.Zero,
        IsSuccess = false,
        ErrorMessage = error ?? "AI provider is not configured",
        GeneratedAt = DateTime.UtcNow
    };
}
