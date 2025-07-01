namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// Available AI models for document processing
/// </summary>
public enum AIModel
{
    /// <summary>
    /// GPT-4.1 - Most capable model
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
    Gpt4oMini
}

/// <summary>
/// Helper methods for AI model handling
/// </summary>
public static class AIModelHelper
{
    /// <summary>
    /// Convert AIModel enum to OpenAI API string
    /// </summary>
    public static string ToApiString(this AIModel model) => model switch
    {
        AIModel.Gpt41 => "gpt-4.1",
        AIModel.Gpt41Mini => "gpt-4.1-mini",
        AIModel.Gpt4o => "gpt-4o",
        AIModel.Gpt4oMini => "gpt-4o-mini",
        _ => "gpt-4o-mini" // Safe default
    };
    
    /// <summary>
    /// Convert AIModel enum to user-friendly display name
    /// </summary>
    public static string ToDisplayName(this AIModel model) => model switch
    {
        AIModel.Gpt41 => "GPT-4.1 (Most Capable)",
        AIModel.Gpt41Mini => "GPT-4.1 Mini (Fast & Efficient)",
        AIModel.Gpt4o => "GPT-4o (Advanced)",
        AIModel.Gpt4oMini => "GPT-4o Mini (Cost Effective)",
        _ => "GPT-4o Mini"
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
        _ => "Balanced option"
    };
    
    /// <summary>
    /// Get all available models for dropdowns
    /// </summary>
    public static AIModel[] GetAllModels() => Enum.GetValues<AIModel>();
    
    /// <summary>
    /// Get default model for new users
    /// </summary>
    public static AIModel GetDefaultModel() => AIModel.Gpt4oMini;
} 