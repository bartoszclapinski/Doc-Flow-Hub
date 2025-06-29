using DocFlowHub.Core.Models.AI;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Core AI service interface for OpenAI integration
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Test connectivity to OpenAI API
    /// </summary>
    Task<bool> TestConnectivityAsync();
    
    /// <summary>
    /// Get AI service health status
    /// </summary>
    Task<AIServiceHealth> GetHealthAsync();
    
    /// <summary>
    /// Generate a completion using the specified model
    /// </summary>
    Task<AIResponse> GenerateCompletionAsync(string prompt, string? model = null);
    
    /// <summary>
    /// Generate a completion with system message and user prompt
    /// </summary>
    Task<AIResponse> GenerateCompletionAsync(string prompt, string systemMessage, string? model = null);
} 