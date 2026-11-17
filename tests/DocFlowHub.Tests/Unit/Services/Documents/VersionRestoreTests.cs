using System.Security.Cryptography;
using System.Text;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Documents;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Documents;

/// <summary>Sprint 22: restoring an older version as a new one.</summary>
public class VersionRestoreTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly DocumentService _service;
    private readonly List<(int Doc, int Version, byte[] Content, string FileName)> _uploads = new();

    private static readonly byte[] V1 = Encoding.UTF8.GetBytes("first text\n");
    private static readonly byte[] V2 = Encoding.UTF8.GetBytes("second text\n");

    public VersionRestoreTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new DocumentService(_context, _storage.Object, Mock.Of<IServiceScopeFactory>(), Mock.Of<ILogger<DocumentService>>());

        _storage.Setup(s => s.DownloadDocumentAsync(1, 1)).ReturnsAsync(ServiceResult<byte[]>.Success(V1));
        _storage.Setup(s => s.DownloadDocumentAsync(1, 2)).ReturnsAsync(ServiceResult<byte[]>.Success(V2));
        _storage.Setup(s => s.UploadDocumentAsync(It.IsAny<IFormFile>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(async (IFormFile file, int doc, int version) =>
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                _uploads.Add((doc, version, ms.ToArray(), file.FileName));
                return ServiceResult<string>.Success($"documents/{doc}/v{version}{Path.GetExtension(file.FileName)}");
            });

        var document = new Document
        {
            Id = 1,
            Title = "Plan",
            FileType = ".txt",
            FilePath = "documents/1/v2.txt",
            FileSize = V2.Length,
            OwnerId = "owner",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
        };
        document.Versions.Add(Version(10, 1, V1));
        document.Versions.Add(Version(11, 2, V2));
        _context.Documents.Add(document);
        _context.SaveChanges();
        document.CurrentVersionId = 11;
        _context.SaveChanges();
    }

    private static DocumentVersion Version(int id, int number, byte[] content) => new()
    {
        Id = id,
        VersionNumber = number,
        FileType = ".txt",
        FileSize = content.Length,
        FilePath = $"documents/1/v{number}.txt",
        CreatedAt = new DateTime(2026, 1, number, 0, 0, 0, DateTimeKind.Utc),
        UserId = "owner",
        ChangeSummary = $"Version {number}",
    };

    [Fact]
    public async Task Restore_AddsANewVersionWithTheOldFile()
    {
        var result = await _service.RestoreVersionAsync(1, 1, "owner");

        result.Succeeded.Should().BeTrue();
        result.Data.Should().Be(3);
        _uploads.Should().ContainSingle();
        _uploads[0].Should().Match<(int Doc, int Version, byte[] Content, string FileName)>(u => u.Doc == 1 && u.Version == 3 && u.FileName.EndsWith(".txt"));
        _uploads[0].Content.Should().Equal(V1);

        var restored = await _context.DocumentVersions.SingleAsync(v => v.DocumentId == 1 && v.VersionNumber == 3);
        restored.ChangeSummary.Should().Be("Restored from v1");
        restored.FileSize.Should().Be(V1.Length);
        restored.FileType.Should().Be(".txt");
        restored.FileHash.Should().Be(Convert.ToHexString(MD5.HashData(V1)));
        restored.UserId.Should().Be("owner");
        restored.FilePath.Should().Be("documents/1/v3.txt");

        var document = await _context.Documents.SingleAsync(d => d.Id == 1);
        document.CurrentVersionId.Should().Be(restored.Id);
        document.FileSize.Should().Be(V1.Length);
        // The older versions stay.
        (await _context.DocumentVersions.CountAsync(v => v.DocumentId == 1)).Should().Be(3);
    }

    [Fact]
    public async Task Restore_KeepsTheUsersDescription()
    {
        (await _service.RestoreVersionAsync(1, 1, "owner", "  Back to the approved text  ")).Succeeded.Should().BeTrue();
        (await _context.DocumentVersions.SingleAsync(v => v.VersionNumber == 3)).ChangeSummary.Should().Be("Back to the approved text");
    }

    [Fact]
    public async Task Restore_IsTheOwners()
    {
        var result = await _service.RestoreVersionAsync(1, 1, "someone-else");

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("owner");
        _uploads.Should().BeEmpty();
    }

    [Theory]
    [InlineData(2, "already the current version")]
    [InlineData(7, "Version not found")]
    public async Task Restore_NeedsAnOlderVersionThatExists(int number, string error)
    {
        var result = await _service.RestoreVersionAsync(1, number, "owner");

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain(error);
        _uploads.Should().BeEmpty();
    }

    [Fact]
    public async Task Restore_OfAnUnknownDocument_Fails()
    {
        (await _service.RestoreVersionAsync(99, 1, "owner")).Error.Should().Be("Document not found");
    }

    [Fact]
    public async Task Restore_WritesNoVersion_WhenTheFileCannotBeReadOrStored()
    {
        _storage.Setup(s => s.DownloadDocumentAsync(1, 1)).ReturnsAsync(ServiceResult<byte[]>.Failure("missing blob"));
        var unreadable = await _service.RestoreVersionAsync(1, 1, "owner");
        unreadable.Succeeded.Should().BeFalse();
        unreadable.Error.Should().Contain("could not be read");

        _storage.Setup(s => s.DownloadDocumentAsync(1, 1)).ReturnsAsync(ServiceResult<byte[]>.Success(V1));
        _storage.Setup(s => s.UploadDocumentAsync(It.IsAny<IFormFile>(), 1, 3)).ReturnsAsync(ServiceResult<string>.Failure("disk full"));
        var unstored = await _service.RestoreVersionAsync(1, 1, "owner");
        unstored.Succeeded.Should().BeFalse();
        unstored.Error.Should().Be("disk full");

        (await _context.DocumentVersions.CountAsync(v => v.DocumentId == 1)).Should().Be(2);
        (await _context.Documents.SingleAsync(d => d.Id == 1)).CurrentVersionId.Should().Be(11);
    }

    [Fact]
    public async Task Restore_ThenAgain_NumbersOnFromTheNewest()
    {
        (await _service.RestoreVersionAsync(1, 1, "owner")).Data.Should().Be(3);
        (await _service.RestoreVersionAsync(1, 2, "owner")).Data.Should().Be(4);
        _uploads.Select(u => u.Version).Should().Equal(3, 4);
        _uploads[1].Content.Should().Equal(V2);
    }
}
