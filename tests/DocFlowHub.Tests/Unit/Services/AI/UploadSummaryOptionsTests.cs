using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.AI;
using DocFlowHub.Infrastructure.Services.Documents;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.AI;

/// <summary>Sprint 23: the upload summary's model and level of detail.</summary>
public class UploadSummaryOptionsTests
{
    [Theory]
    [InlineData(0.1, SummaryDetail.Short)]
    [InlineData(0.44, SummaryDetail.Short)]
    [InlineData(0.45, SummaryDetail.Standard)]
    [InlineData(0.7, SummaryDetail.Standard)]
    [InlineData(0.8, SummaryDetail.Detailed)]
    [InlineData(1.0, SummaryDetail.Detailed)]
    public void QualityPreference_MapsToADetail(double quality, SummaryDetail detail)
    {
        SummaryDetailHelper.FromQuality(quality).Should().Be(detail);
    }

    [Fact]
    public void EveryDetail_SurvivesARoundTripThroughTheStoredQuality()
    {
        foreach (var detail in Enum.GetValues<SummaryDetail>())
        {
            SummaryDetailHelper.FromQuality(SummaryDetailHelper.ToQuality(detail)).Should().Be(detail);
        }
        // The old default (0.7) stays "Standard": nobody's summaries change without a choice.
        SummaryDetailHelper.FromQuality(new AISettings().QualityPreference).Should().Be(SummaryDetail.Standard);
    }

    [Fact]
    public void EveryModel_HasFiguresForTheEstimate()
    {
        foreach (var model in AIModelHelper.GetAllModels())
        {
            var (input, output) = AIModelFacts.PricePerMillionTokens(model);
            input.Should().BePositive();
            output.Should().BeGreaterThan(input);
            AIModelFacts.TypicalSeconds(model).Should().BePositive();
            AIModelFacts.Strengths(model).Should().NotBeNullOrWhiteSpace();
        }
        SummaryDetailHelper.TypicalOutputTokens(SummaryDetail.Short)
            .Should().BeLessThan(SummaryDetailHelper.TypicalOutputTokens(SummaryDetail.Standard));
        SummaryDetailHelper.TypicalOutputTokens(SummaryDetail.Standard)
            .Should().BeLessThan(SummaryDetailHelper.TypicalOutputTokens(SummaryDetail.Detailed));
    }

    [Theory]
    [InlineData(SummaryDetail.Short, "A one-sentence summary", "3 short points")]
    [InlineData(SummaryDetail.Standard, "A concise summary (2-3 sentences)", "3-5 main points")]
    [InlineData(SummaryDetail.Detailed, "A detailed summary (one paragraph of 5-7 sentences)", "6-8 points")]
    public void ThePrompt_FollowsTheDetail_AndKeepsTheShapeTheParserReads(SummaryDetail detail, string summary, string points)
    {
        var prompt = DocumentSummaryService.BuildPrompt("Budget 2026", "The budget grows by 4%.", detail);

        prompt.Should().Contain("Document Title: Budget 2026");
        prompt.Should().Contain("The budget grows by 4%.");
        prompt.Should().Contain("1. " + summary);
        prompt.Should().Contain("2. Key points (bullet format, ").And.Contain(points);
        prompt.TrimEnd().Should().EndWith("Summary:");
    }

    [Fact]
    public async Task AnUpload_SummarisesWithTheChosenModelAndDetail()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var context = new ApplicationDbContext(options);

        var storage = new Mock<IDocumentStorageService>();
        storage.Setup(s => s.UploadDocumentAsync(It.IsAny<IFormFile>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(ServiceResult<string>.Success("documents/1/v1.txt"));

        var calls = new TaskCompletionSource<(int Doc, AIModel Model, SummaryDetail Detail)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var summaries = new Mock<IDocumentSummaryService>();
        summaries.Setup(s => s.RegenerateSummaryAsync(It.IsAny<int>(), It.IsAny<AIModel>(), It.IsAny<SummaryDetail>()))
            .Callback((int doc, AIModel model, SummaryDetail detail) => calls.TrySetResult((doc, model, detail)))
            .ReturnsAsync(ServiceResult.Success());

        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(IDocumentSummaryService))).Returns(summaries.Object);
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider.Object);
        var scopes = new Mock<IServiceScopeFactory>();
        scopes.Setup(f => f.CreateScope()).Returns(scope.Object);

        var service = new DocumentService(context, storage.Object, scopes.Object, Mock.Of<ILogger<DocumentService>>());
        var file = new FormFile(new MemoryStream("hello"u8.ToArray()), 0, 5, "file", "notes.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain",
        };

        var result = await service.CreateDocumentAsync(new CreateDocumentRequest
        {
            Title = "Notes",
            OwnerId = "owner",
            GenerateSummary = true,
            SummaryModel = AIModel.ClaudeSonnet5,
            SummaryDetail = SummaryDetail.Detailed,
        }, file);

        result.Succeeded.Should().BeTrue();
        var call = await calls.Task.WaitAsync(TimeSpan.FromSeconds(10));
        call.Should().Be((result.Data!.Id, AIModel.ClaudeSonnet5, SummaryDetail.Detailed));
    }
}
