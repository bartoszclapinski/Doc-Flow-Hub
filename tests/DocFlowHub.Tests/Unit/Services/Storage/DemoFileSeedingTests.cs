using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.AI;
using DocFlowHub.Infrastructure.Services.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Storage;

/// <summary>
/// The demo seed generates real files so downloads work in the public demo. These tests
/// prove each generated file passes the same checks as a user upload (whitelist +
/// signature) and that its text can be extracted back.
/// </summary>
public class DemoFileSeedingTests
{
    private static readonly string[] Lines =
    {
        "Demo PDF document — Budget Plan 2025-03.",
        "Version 2: Revision 2 (draft)",
        "The goal of this document is to give the team one clear reference point. It describes the current situation, the agreed approach and the next milestones.",
    };

    public static TheoryData<string> Types()
    {
        var data = new TheoryData<string>();
        foreach (var t in DemoFileFactory.SupportedTypes) data.Add(t);
        return data;
    }

    [Fact]
    public void SupportedTypes_AreAllAllowedByUploadWhitelist()
    {
        var allowed = new DocumentStorageOptions().AllowedFileTypes;

        DemoFileFactory.SupportedTypes.Select(t => "." + t).Should().BeSubsetOf(allowed);
    }

    [Theory]
    [MemberData(nameof(Types))]
    public async Task Create_ProducesFileThatPassesSignatureCheck(string type)
    {
        var bytes = DemoFileFactory.Create(type, "Budget Plan 2025-03", Lines);
        var file = DemoFileFactory.ToFormFile(bytes, $"Budget Plan 2025-03.{type}", type);

        (await FileSignatureValidator.IsContentValidAsync(file, "." + type)).Should().BeTrue();
        file.ContentType.Should().Be(DemoFileFactory.GetContentType(type));
    }

    [Theory]
    [MemberData(nameof(Types))]
    public async Task Create_ProducesFileWhoseTextCanBeExtracted(string type)
    {
        var extractor = CreateExtractor();
        var bytes = DemoFileFactory.Create(type, "Budget Plan 2025-03", Lines);

        var result = await extractor.ExtractTextAsync(bytes, $"doc.{type}", DemoFileFactory.GetContentType(type));

        result.Succeeded.Should().BeTrue(result.Error);
        result.Data.Should().Contain("Budget Plan 2025-03");
        result.Data.Should().Contain("(draft)");
        // Last words of the long line: proves PDF wrapping keeps the whole sentence.
        result.Data.Should().Contain("next milestones");
    }

    [Fact]
    public void Create_AcceptsDottedFileType_AsStoredByTheApp()
    {
        DemoFileFactory.IsSupported(".pdf").Should().BeTrue();
        DemoFileFactory.IsSupported(".xlsx").Should().BeFalse();
        DemoFileFactory.GetContentType(".docx").Should().Be(DemoFileFactory.GetContentType("docx"));
        DemoFileFactory.Create(".md", "T", Lines).Should().Equal(DemoFileFactory.Create("md", "T", Lines));
    }

    [Fact]
    public async Task UploadFilesAsync_UploadsEveryVersion_AndStoresRealSizeAndHash()
    {
        await using var context = CreateContext();
        var doc = await AddDocumentAsync(context, versions: 2);
        var (storage, uploaded) = RecordingStorage();

        var count = await DemoDataSeeder.UploadFilesAsync(context, storage.Object, NullLogger.Instance, new[] { doc });

        count.Should().Be(2);
        uploaded.Select(u => u.Version).Should().Equal(1, 2);
        uploaded.Should().OnlyContain(u => u.Doc == doc.Id && u.FileName == "Roadmap 2025-04.pdf");
        var saved = await context.DocumentVersions.Where(v => v.DocumentId == doc.Id).OrderBy(v => v.VersionNumber).ToListAsync();
        saved.Select(v => v.FileSize).Should().Equal(uploaded.Select(u => (long)u.Content.Length));
        saved.Select(v => v.FileHash).Should().Equal(uploaded.Select(u => DemoFileFactory.ComputeHash(u.Content)));
        (await context.Documents.FindAsync(doc.Id))!.FileSize.Should().Be(uploaded[^1].Content.Length);
    }

    [Fact]
    public async Task UploadFilesAsync_StopsAtFirstFailure_WithoutThrowing()
    {
        await using var context = CreateContext();
        var doc = await AddDocumentAsync(context, versions: 3);
        var storage = new Mock<IDocumentStorageService>();
        storage.Setup(s => s.UploadDocumentAsync(It.IsAny<IFormFile>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(ServiceResult<string>.Failure("storage unreachable"));

        var count = await DemoDataSeeder.UploadFilesAsync(context, storage.Object, NullLogger.Instance, new[] { doc });

        count.Should().Be(0);
        storage.Verify(s => s.UploadDocumentAsync(It.IsAny<IFormFile>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task Backfill_WhenNewestFileExists_UploadsNothing()
    {
        await using var context = CreateContext();
        await AddDocumentAsync(context, versions: 1);
        var newest = await AddDocumentAsync(context, versions: 2);
        var (storage, uploaded) = RecordingStorage();
        storage.Setup(s => s.DocumentExistsAsync(newest.Id, 2)).ReturnsAsync(ServiceResult<bool>.Success(true));

        await DemoDataSeeder.BackfillMissingFilesAsync(context, storage.Object, NullLogger.Instance);

        uploaded.Should().BeEmpty();
        storage.Verify(s => s.DocumentExistsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task Backfill_WhenNewestFileMissing_UploadsAllSupported_AndSkipsLegacyXlsx()
    {
        await using var context = CreateContext();
        var pdf = await AddDocumentAsync(context, versions: 2);
        // Newest doc is a legacy xlsx from the earlier seeder: it must neither be the probe
        // (it never gets a file, so the backfill would run on every startup) nor crash.
        var xlsx = await AddDocumentAsync(context, versions: 1, fileType: ".xlsx");
        var (storage, uploaded) = RecordingStorage();
        storage.Setup(s => s.DocumentExistsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(ServiceResult<bool>.Success(false));

        await DemoDataSeeder.BackfillMissingFilesAsync(context, storage.Object, NullLogger.Instance);

        storage.Verify(s => s.DocumentExistsAsync(pdf.Id, 2), Times.Once);
        uploaded.Select(u => (u.Doc, u.Version)).Should().Equal((pdf.Id, 1), (pdf.Id, 2));
        uploaded.Should().NotContain(u => u.Doc == xlsx.Id);
    }

    [Fact]
    public async Task NormalizeLegacyFileTypes_AddsLeadingDot_ToDemoRowsOnly()
    {
        await using var context = CreateContext();
        var legacy = await AddDocumentAsync(context, versions: 2, fileType: "pdf");
        var userDoc = await AddDocumentAsync(context, versions: 1, fileType: "txt", filePathPrefix: "uploads/");

        await DemoDataSeeder.NormalizeLegacyFileTypesAsync(context);

        var fixedDoc = await context.Documents.Include(d => d.Versions).SingleAsync(d => d.Id == legacy.Id);
        fixedDoc.FileType.Should().Be(".pdf");
        fixedDoc.Versions.Should().OnlyContain(v => v.FileType == ".pdf");
        (await context.Documents.FindAsync(userDoc.Id))!.FileType.Should().Be("txt");
    }

    private static (Mock<IDocumentStorageService> Storage, List<(int Doc, int Version, string FileName, byte[] Content)> Uploaded) RecordingStorage()
    {
        var uploaded = new List<(int, int, string, byte[])>();
        var storage = new Mock<IDocumentStorageService>();
        storage.Setup(s => s.UploadDocumentAsync(It.IsAny<IFormFile>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<IFormFile, int, int>((f, d, v) =>
            {
                using var ms = new MemoryStream();
                f.CopyTo(ms);
                uploaded.Add((d, v, f.FileName, ms.ToArray()));
            })
            .ReturnsAsync(ServiceResult<string>.Success("ok"));
        return (storage, uploaded);
    }

    private static TextExtractionService CreateExtractor() =>
        new(new Mock<IDocumentStorageService>().Object, CreateContext(), new Mock<ILogger<TextExtractionService>>().Object);

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Document> AddDocumentAsync(
        ApplicationDbContext context, int versions, string fileType = ".pdf", string filePathPrefix = "demo/")
    {
        var doc = new Document
        {
            Title = "Roadmap 2025-04",
            Description = "Demo PDF document — Roadmap 2025-04.",
            FileType = fileType,
            FilePath = $"{filePathPrefix}x/v1{fileType}",
            FileSize = 12345,
            OwnerId = "owner",
            CreatedAt = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        for (var v = 1; v <= versions; v++)
        {
            doc.Versions.Add(new DocumentVersion
            {
                VersionNumber = v,
                FileType = fileType,
                FilePath = $"{filePathPrefix}x/v{v}{fileType}",
                StoragePath = $"{filePathPrefix}x/v{v}{fileType}",
                FileSize = 999,
                UserName = "Demo User",
                ChangeSummary = v == 1 ? "Initial version" : $"Revision {v}",
                CreatedAt = doc.CreatedAt.AddDays(v),
                UserId = "owner",
                CreatedBy = "owner",
            });
        }
        context.Documents.Add(doc);
        await context.SaveChangesAsync();
        doc.CurrentVersionId = doc.Versions.OrderByDescending(v => v.VersionNumber).First().Id;
        await context.SaveChangesAsync();
        return doc;
    }
}
