namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// AI providers supported by the application
/// </summary>
public enum AIProvider
{
    OpenAI,
    Anthropic
}

/// <summary>
/// Available AI models for document processing.
/// IMPORTANT: PreferredModel is persisted as an int (see AISettingsConfiguration) —
/// only APPEND new values, never reorder or remove existing ones.
/// </summary>
public enum AIModel
{
    /// <summary>
    /// GPT-4.1 - Most capable OpenAI model
    /// </summary>
    Gpt41,

    /// <summary>
    /// GPT-4.1 Mini - Fast and efficient version
    /// </summary>
    Gpt41Mini,

    /// <summary>
    /// GPT-4o - Advanced model
    /// </summary>
    Gpt4o,

    /// <summary>
    /// GPT-4o Mini - Cost-effective option
    /// </summary>
    Gpt4oMini,

    /// <summary>
    /// Claude Haiku 4.5 - Fastest and most cost-effective Claude model (default)
    /// </summary>
    ClaudeHaiku45,

    /// <summary>
    /// Claude Sonnet 5 - Near-Opus quality at Sonnet cost
    /// </summary>
    ClaudeSonnet5,

    /// <summary>
    /// Claude Opus 4.8 - Most capable Claude model
    /// </summary>
    ClaudeOpus48
}

/// <summary>
/// Helper methods for AI model handling
/// </summary>
public static class AIModelHelper
{
    /// <summary>
    /// Convert AIModel enum to the provider API model string
    /// </summary>
    public static string ToApiString(this AIModel model) => model switch
    {
        AIModel.Gpt41 => "gpt-4.1",
        AIModel.Gpt41Mini => "gpt-4.1-mini",
        AIModel.Gpt4o => "gpt-4o",
        AIModel.Gpt4oMini => "gpt-4o-mini",
        AIModel.ClaudeHaiku45 => "claude-haiku-4-5",
        AIModel.ClaudeSonnet5 => "claude-sonnet-5",
        AIModel.ClaudeOpus48 => "claude-opus-4-8",
        _ => "claude-haiku-4-5" // Safe default
    };

    /// <summary>
    /// Parse a provider API model string back to the AIModel enum.
    /// Returns the application default when the string is unknown.
    /// </summary>
    public static AIModel FromApiString(string? apiString)
    {
        TryFromApiString(apiString, out var model);
        return model;
    }

    /// <summary>
    /// Parse a provider API model string, reporting whether it was recognized.
    /// Unlike <see cref="FromApiString"/> (which silently falls back to the default), this
    /// lets callers distinguish "user picked an unknown/stale value" from "user picked the
    /// default model" — so e.g. a stale dropdown value can fall back to the user's own
    /// preference instead of the global default. <paramref name="model"/> is set to the
    /// application default when the string is unrecognized.
    /// </summary>
    public static bool TryFromApiString(string? apiString, out AIModel model)
    {
        switch (apiString?.ToLowerInvariant())
        {
            case "gpt-4.1": model = AIModel.Gpt41; return true;
            case "gpt-4.1-mini": model = AIModel.Gpt41Mini; return true;
            case "gpt-4o": model = AIModel.Gpt4o; return true;
            case "gpt-4o-mini": model = AIModel.Gpt4oMini; return true;
            case "gpt-4-turbo": model = AIModel.Gpt4o; return true; // Legacy mapping
            case "claude-haiku-4-5": model = AIModel.ClaudeHaiku45; return true;
            case "claude-sonnet-5": model = AIModel.ClaudeSonnet5; return true;
            case "claude-opus-4-8": model = AIModel.ClaudeOpus48; return true;
            default: model = GetDefaultModel(); return false;
        }
    }

    /// <summary>
    /// Convert AIModel enum to user-friendly display name
    /// </summary>
    public static string ToDisplayName(this AIModel model) => model switch
    {
        AIModel.Gpt41 => "GPT-4.1 (Most Capable GPT)",
        AIModel.Gpt41Mini => "GPT-4.1 Mini (Fast & Efficient)",
        AIModel.Gpt4o => "GPT-4o (Advanced)",
        AIModel.Gpt4oMini => "GPT-4o Mini (Cost Effective)",
        AIModel.ClaudeHaiku45 => "Claude Haiku 4.5 (Fast & Cost Effective)",
        AIModel.ClaudeSonnet5 => "Claude Sonnet 5 (Balanced)",
        AIModel.ClaudeOpus48 => "Claude Opus 4.8 (Most Capable)",
        _ => "Claude Haiku 4.5"
    };

    /// <summary>
    /// Get cost-effectiveness description for user guidance
    /// </summary>
    public static string GetCostDescription(this AIModel model) => model switch
    {
        AIModel.Gpt41 => "Highest quality, highest cost",
        AIModel.Gpt41Mini => "High quality, moderate cost",
        AIModel.Gpt4o => "Good quality, moderate cost",
        AIModel.Gpt4oMini => "Good quality, lowest cost",
        AIModel.ClaudeHaiku45 => "Great quality, lowest Claude cost",
        AIModel.ClaudeSonnet5 => "Near-Opus quality, moderate cost",
        AIModel.ClaudeOpus48 => "Highest Claude quality, highest cost",
        _ => "Balanced option"
    };

    /// <summary>
    /// Which provider serves this model
    /// </summary>
    public static AIProvider GetProvider(this AIModel model) => model switch
    {
        AIModel.ClaudeHaiku45 or AIModel.ClaudeSonnet5 or AIModel.ClaudeOpus48 => AIProvider.Anthropic,
        _ => AIProvider.OpenAI
    };

    /// <summary>
    /// Which provider serves this API model string (used for request routing)
    /// </summary>
    public static AIProvider GetProviderForApiString(string? apiString)
    {
        // Null OR empty/whitespace both mean "no explicit model" -> the default model's
        // provider. (Treating "" as OpenAI here while FromApiString("") returns the Claude
        // default routed an empty model to the wrong provider.)
        if (string.IsNullOrWhiteSpace(apiString))
        {
            return GetDefaultModel().GetProvider();
        }

        return apiString.StartsWith("claude", StringComparison.OrdinalIgnoreCase)
            ? AIProvider.Anthropic
            : AIProvider.OpenAI;
    }

    /// <summary>
    /// Get all available models for dropdowns
    /// </summary>
    public static AIModel[] GetAllModels() => Enum.GetValues<AIModel>();

    /// <summary>
    /// Get default model for new users (Claude Haiku 4.5 — fast and cost-effective)
    /// </summary>
    public static AIModel GetDefaultModel() => AIModel.ClaudeHaiku45;
}
