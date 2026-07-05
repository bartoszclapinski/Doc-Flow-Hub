using DocFlowHub.Core.Models.AI;
using FluentAssertions;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.AI;

/// <summary>
/// Tests for the model↔provider mapping that drives AIServiceRouter dispatch.
/// PreferredModel is persisted as an int, so enum positions are load-bearing —
/// these tests pin them against accidental reordering.
/// </summary>
public class AIModelHelperTests
{
    [Fact]
    public void EnumValues_ArePinned_BecausePersistedAsInt()
    {
        // Existing values (DB rows depend on these ints)
        ((int)AIModel.Gpt41).Should().Be(0);
        ((int)AIModel.Gpt41Mini).Should().Be(1);
        ((int)AIModel.Gpt4o).Should().Be(2);
        ((int)AIModel.Gpt4oMini).Should().Be(3);
        // Appended Claude values
        ((int)AIModel.ClaudeHaiku45).Should().Be(4);
        ((int)AIModel.ClaudeSonnet5).Should().Be(5);
        ((int)AIModel.ClaudeOpus48).Should().Be(6);
    }

    [Theory]
    [InlineData(AIModel.ClaudeHaiku45, "claude-haiku-4-5")]
    [InlineData(AIModel.ClaudeSonnet5, "claude-sonnet-5")]
    [InlineData(AIModel.ClaudeOpus48, "claude-opus-4-8")]
    [InlineData(AIModel.Gpt4oMini, "gpt-4o-mini")]
    public void ToApiString_MapsToProviderModelIds(AIModel model, string expected)
    {
        model.ToApiString().Should().Be(expected);
    }

    [Theory]
    [InlineData("claude-haiku-4-5", AIModel.ClaudeHaiku45)]
    [InlineData("CLAUDE-OPUS-4-8", AIModel.ClaudeOpus48)]
    [InlineData("gpt-4o-mini", AIModel.Gpt4oMini)]
    [InlineData("gpt-4-turbo", AIModel.Gpt4o)] // legacy mapping preserved
    public void FromApiString_RoundTripsKnownModels(string apiString, AIModel expected)
    {
        AIModelHelper.FromApiString(apiString).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("some-unknown-model")]
    public void FromApiString_UnknownFallsBackToDefault(string? apiString)
    {
        AIModelHelper.FromApiString(apiString).Should().Be(AIModelHelper.GetDefaultModel());
    }

    [Fact]
    public void DefaultModel_IsClaudeHaiku45()
    {
        AIModelHelper.GetDefaultModel().Should().Be(AIModel.ClaudeHaiku45);
        AISettingsHelper.GetDefaultSettings("user-1").PreferredModel.Should().Be(AIModel.ClaudeHaiku45);
    }

    [Theory]
    [InlineData(AIModel.ClaudeHaiku45, AIProvider.Anthropic)]
    [InlineData(AIModel.ClaudeSonnet5, AIProvider.Anthropic)]
    [InlineData(AIModel.ClaudeOpus48, AIProvider.Anthropic)]
    [InlineData(AIModel.Gpt41, AIProvider.OpenAI)]
    [InlineData(AIModel.Gpt4oMini, AIProvider.OpenAI)]
    public void GetProvider_RoutesModelsToCorrectProvider(AIModel model, AIProvider expected)
    {
        model.GetProvider().Should().Be(expected);
    }

    [Theory]
    [InlineData("claude-haiku-4-5", AIProvider.Anthropic)]
    [InlineData("Claude-Sonnet-5", AIProvider.Anthropic)]   // case-insensitive
    [InlineData("gpt-4o-mini", AIProvider.OpenAI)]
    [InlineData("anything-else", AIProvider.OpenAI)]
    public void GetProviderForApiString_RoutesByPrefix(string apiString, AIProvider expected)
    {
        AIModelHelper.GetProviderForApiString(apiString).Should().Be(expected);
    }

    [Fact]
    public void GetProviderForApiString_NullUsesDefaultModelProvider()
    {
        // null model → the default model's provider (Haiku → Anthropic)
        AIModelHelper.GetProviderForApiString(null).Should().Be(AIProvider.Anthropic);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetProviderForApiString_EmptyMatchesFromApiString_UsesDefaultProvider(string apiString)
    {
        // Empty/whitespace must route like null (default model's provider), consistent with
        // FromApiString("") returning the default model — otherwise "" would route to OpenAI
        // while the resolved model is Claude.
        AIModelHelper.GetProviderForApiString(apiString).Should().Be(AIProvider.Anthropic);
        AIModelHelper.GetProviderForApiString(apiString)
            .Should().Be(AIModelHelper.FromApiString(apiString).GetProvider());
    }

    [Theory]
    [InlineData("claude-haiku-4-5", AIModel.ClaudeHaiku45)]
    [InlineData("GPT-4O-MINI", AIModel.Gpt4oMini)]
    [InlineData("gpt-4-turbo", AIModel.Gpt4o)] // legacy alias still recognized
    public void TryFromApiString_KnownModels_ReturnsTrue(string apiString, AIModel expected)
    {
        AIModelHelper.TryFromApiString(apiString, out var model).Should().BeTrue();
        model.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("gpt-3.5-turbo")]   // stale/unsupported value
    [InlineData("some-nonsense")]
    public void TryFromApiString_UnknownModels_ReturnsFalseAndDefault(string? apiString)
    {
        AIModelHelper.TryFromApiString(apiString, out var model).Should().BeFalse();
        model.Should().Be(AIModelHelper.GetDefaultModel());
    }

    [Fact]
    public void EveryModel_HasDistinctApiString_AndRoundTrips()
    {
        var models = AIModelHelper.GetAllModels();
        var apiStrings = models.Select(m => m.ToApiString()).ToList();

        apiStrings.Should().OnlyHaveUniqueItems();
        foreach (var model in models)
        {
            AIModelHelper.FromApiString(model.ToApiString()).Should().Be(model);
        }
    }

    [Fact]
    public void EveryModel_HasDisplayName_CostDescription_AndProvider()
    {
        // Guards the display/description/provider switches against a future enum value
        // being added without extending them (the class of bug the PR review found).
        foreach (var model in AIModelHelper.GetAllModels())
        {
            model.ToDisplayName().Should().NotBeNullOrWhiteSpace();
            model.GetCostDescription().Should().NotBeNullOrWhiteSpace();
            model.GetProvider().Should().BeOneOf(AIProvider.OpenAI, AIProvider.Anthropic);
            // No display name should still say "GPT" for a Claude model, or vice versa.
            if (model.GetProvider() == AIProvider.Anthropic)
            {
                model.ToDisplayName().Should().Contain("Claude");
            }
        }
    }
}
