namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// How much an AI summary says (Sprint 23). It replaces the old 0–1 "quality" slider, which nothing read:
/// the detail changes the prompt, so it is a real choice.
/// </summary>
public enum SummaryDetail
{
    /// <summary>One sentence and three short key points.</summary>
    Short,

    /// <summary>Two or three sentences and three to five key points (the summary the app always made).</summary>
    Standard,

    /// <summary>A paragraph and six to eight key points.</summary>
    Detailed,
}

public static class SummaryDetailHelper
{
    /// <summary>The stored <c>AISettings.QualityPreference</c> (0–1) as a detail, so old settings keep a meaning.</summary>
    public static SummaryDetail FromQuality(double quality) => quality switch
    {
        < 0.45 => SummaryDetail.Short,
        < 0.8 => SummaryDetail.Standard,
        _ => SummaryDetail.Detailed,
    };

    /// <summary>The value saved to <c>QualityPreference</c> when the user remembers a detail as the default.</summary>
    public static double ToQuality(SummaryDetail detail) => detail switch
    {
        SummaryDetail.Short => 0.3,
        SummaryDetail.Detailed => 0.9,
        _ => 0.7,
    };

    public static string Describe(SummaryDetail detail) => detail switch
    {
        SummaryDetail.Short => "One sentence and 3 short key points",
        SummaryDetail.Detailed => "A full paragraph and 6–8 key points",
        _ => "2–3 sentences and 3–5 key points",
    };

    /// <summary>About how many tokens the answer takes, for the cost estimate.</summary>
    public static int TypicalOutputTokens(SummaryDetail detail) => detail switch
    {
        SummaryDetail.Short => 150,
        SummaryDetail.Detailed => 700,
        _ => 350,
    };
}

/// <summary>
/// Figures for the upload screen's estimate (Sprint 23). The prices are the providers' list prices per
/// million tokens when this was written; the page labels everything as an estimate.
/// </summary>
public static class AIModelFacts
{
    public static (decimal Input, decimal Output) PricePerMillionTokens(AIModel model) => model switch
    {
        AIModel.ClaudeHaiku45 => (1m, 5m),
        AIModel.ClaudeSonnet5 => (3m, 15m),
        AIModel.ClaudeOpus48 => (5m, 25m),
        AIModel.Gpt41 => (2m, 8m),
        AIModel.Gpt41Mini => (0.4m, 1.6m),
        AIModel.Gpt4o => (2.5m, 10m),
        AIModel.Gpt4oMini => (0.15m, 0.6m),
        _ => (1m, 5m),
    };

    /// <summary>Seconds a standard summary of a short document usually takes.</summary>
    public static int TypicalSeconds(AIModel model) => model switch
    {
        AIModel.ClaudeHaiku45 => 5,
        AIModel.Gpt4oMini => 6,
        AIModel.Gpt41Mini => 8,
        AIModel.ClaudeSonnet5 or AIModel.Gpt4o => 10,
        AIModel.Gpt41 => 14,
        AIModel.ClaudeOpus48 => 16,
        _ => 10,
    };

    public static string Strengths(AIModel model) => model switch
    {
        AIModel.ClaudeHaiku45 => "Fast and inexpensive; good for everyday summaries",
        AIModel.ClaudeSonnet5 => "Close to the top model at a moderate price; good for most documents",
        AIModel.ClaudeOpus48 => "The most capable Claude model; for long or critical documents",
        AIModel.Gpt41 => "OpenAI's most capable model here; for complex documents",
        AIModel.Gpt41Mini => "Good quality at a low price",
        AIModel.Gpt4o => "Balanced speed and quality",
        AIModel.Gpt4oMini => "The cheapest option; fine for short, simple documents",
        _ => "AI document summaries",
    };
}
