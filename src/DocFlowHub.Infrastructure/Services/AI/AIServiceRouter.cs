using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.AI;

namespace DocFlowHub.Infrastructure.Services.AI;

/// <summary>
/// Routes AI requests to the correct provider (OpenAI or Anthropic).
/// Completion requests route by the requested model string ("claude-*" → Anthropic,
/// otherwise OpenAI; null → the application default model's provider). Health and
/// connectivity checks probe whichever provider(s) are actually configured — NOT just
/// the default — so an OpenAI-only (or Anthropic-only) deployment reports correctly.
/// Provider services are resolved lazily; a missing key for a provider the user never
/// selects surfaces only as a failed AIResponse, never a startup crash.
/// </summary>
public class AIServiceRouter : IAIService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIServiceRouter> _logger;

    public AIServiceRouter(IServiceProvider services, IConfiguration configuration, ILogger<AIServiceRouter> logger)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> TestConnectivityAsync()
    {
        var providers = ConfiguredProviders().ToList();
        if (providers.Count == 0)
        {
            return false;
        }

        foreach (var provider in providers)
        {
            var (service, _) = Resolve(provider);
            if (service is not null && await service.TestConnectivityAsync())
            {
                return true; // healthy if any configured provider connects
            }
        }

        return false;
    }

    public async Task<AIServiceHealth> GetHealthAsync()
    {
        var providers = ConfiguredProviders().ToList();
        if (providers.Count == 0)
        {
            return new AIServiceHealth
            {
                IsHealthy = false,
                ResponseTime = TimeSpan.Zero,
                CheckedAt = DateTime.UtcNow,
                Status = "No AI provider is configured (set DocFlowHub:AnthropicApiKey or OpenAI:ApiKey)"
            };
        }

        AIServiceHealth? lastResult = null;
        foreach (var provider in providers)
        {
            var (service, error) = Resolve(provider);
            if (service is null)
            {
                lastResult = Unhealthy(provider, error);
                continue;
            }

            var health = await service.GetHealthAsync();
            health.Status = $"[{provider}] {health.Status}";
            if (health.IsHealthy)
            {
                return health; // first healthy provider wins
            }
            lastResult = health;
        }

        return lastResult!; // no provider healthy — surface the last error
    }

    public async Task<AIResponse> GenerateCompletionAsync(string prompt, string? model = null)
    {
        var (service, error, effectiveModel) = ResolveForCompletion(model);
        return service is null
            ? Failure(model, error)
            : await service.GenerateCompletionAsync(prompt, effectiveModel);
    }

    public async Task<AIResponse> GenerateCompletionAsync(string prompt, string systemMessage, string? model = null)
    {
        var (service, error, effectiveModel) = ResolveForCompletion(model);
        return service is null
            ? Failure(model, error)
            : await service.GenerateCompletionAsync(prompt, systemMessage, effectiveModel);
    }

    /// <summary>
    /// Resolve the provider for a completion request, with a single-provider fallback: if the
    /// model's own provider has no key configured but the other provider does, serve the request
    /// on that provider using its default model. This keeps default AI features working on a
    /// deployment that only has one provider's key (e.g. OpenAI-only), instead of hard-failing
    /// just because the global default model belongs to the unconfigured provider.
    /// </summary>
    private (IAIService? Service, string? Error, string? Model) ResolveForCompletion(string? model)
    {
        var preferred = AIModelHelper.GetProviderForApiString(model);
        if (IsConfigured(preferred))
        {
            var (service, error) = Resolve(preferred);
            return (service, error, model);
        }

        var fallback = Other(preferred);
        if (IsConfigured(fallback))
        {
            _logger.LogWarning(
                "Requested AI provider {Preferred} is not configured; falling back to {Fallback} with its default model.",
                preferred, fallback);
            var (service, error) = Resolve(fallback);
            // Drop the cross-provider model string so the fallback provider uses its own default.
            return (service, error, null);
        }

        return (null, "No AI provider is configured (set DocFlowHub:AnthropicApiKey or OpenAI:ApiKey)", model);
    }

    /// <summary>
    /// The providers that have an API key configured. Health checks only probe these,
    /// so a valid OpenAI key isn't reported invalid just because Anthropic is the default.
    /// </summary>
    private IEnumerable<AIProvider> ConfiguredProviders()
    {
        // Probe the application default's provider first so the "primary" path is checked first.
        var defaultProvider = AIModelHelper.GetDefaultModel().GetProvider();
        foreach (var provider in new[] { defaultProvider, Other(defaultProvider) })
        {
            if (IsConfigured(provider))
            {
                yield return provider;
            }
        }
    }

    private bool IsConfigured(AIProvider provider)
    {
        var key = provider == AIProvider.Anthropic
            ? _configuration["DocFlowHub:AnthropicApiKey"] ?? _configuration["Anthropic:ApiKey"]
            : _configuration["OpenAI:ApiKey"];
        return !string.IsNullOrWhiteSpace(key);
    }

    private static AIProvider Other(AIProvider provider)
        => provider == AIProvider.Anthropic ? AIProvider.OpenAI : AIProvider.Anthropic;

    /// <summary>
    /// Resolve the provider service. Construction can throw (missing API key) — converted
    /// here into a (null, error) result so callers get a clean failed AIResponse, not a 500.
    /// </summary>
    private (IAIService? Service, string? Error) Resolve(AIProvider provider)
    {
        try
        {
            IAIService service = provider == AIProvider.Anthropic
                ? _services.GetRequiredService<ClaudeAIService>()
                : _services.GetRequiredService<OpenAIService>();
            return (service, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve AI provider {Provider}", provider);
            return (null, $"{provider} is not configured: {ex.Message}");
        }
    }

    private static AIServiceHealth Unhealthy(AIProvider provider, string? error) => new()
    {
        IsHealthy = false,
        ResponseTime = TimeSpan.Zero,
        CheckedAt = DateTime.UtcNow,
        Status = $"[{provider}] not configured",
        ErrorMessage = error
    };

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
