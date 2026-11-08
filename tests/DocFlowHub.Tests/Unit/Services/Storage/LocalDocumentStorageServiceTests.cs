using System.Text;
using DocFlowHub.Infrastructure.Services.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Storage;

/// <summary>Disk-based storage used by the self-contained demo container.</summary>
public sealed class LocalDocumentStorageServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"docflowhub-local-{Guid.NewGuid():N}");
    private readonly LocalDocumentStorageService _storage;

    public LocalDocumentStorageServiceTests()
    {
        _storage = new LocalDocumentStorageService(
            Options.Create(new DocumentStorageOptions { LocalPath = _root }),
            NullLogger<LocalDocumentStorageService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static IFormFile TextFile(string content, string name = "notes.txt")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain",
        };
    }

    [Fact]
    public async Task Upload_ThenDownload_ReturnsTheSameBytes()
    {
        var upload = await _storage.UploadDocumentAsync(TextFile("hello demo"), documentId: 7, versionNumber: 2);
        upload.Succeeded.Should().BeTrue(upload.Error);
        upload.Data.Should().Be("7/2");

        (await _storage.DocumentExistsAsync(7, 2)).Data.Should().BeTrue();
        var download = await _storage.DownloadDocumentAsync(7, 2);
        Encoding.UTF8.GetString(download.Data!).Should().Be("hello demo");
    }

    [Fact]
    public async Task Upload_OverwritesTheSameVersion()
    {
        await _storage.UploadDocumentAsync(TextFile("first"), 1, 1);
        await _storage.UploadDocumentAsync(TextFile("second"), 1, 1);

        Encoding.UTF8.GetString((await _storage.DownloadDocumentAsync(1, 1)).Data!).Should().Be("second");
    }

    [Fact]
    public async Task Upload_RejectsDisallowedType()
    {
        var result = await _storage.UploadDocumentAsync(TextFile("MZ", "tool.exe"), 1, 1);

        result.Succeeded.Should().BeFalse();
        (await _storage.DocumentExistsAsync(1, 1)).Data.Should().BeFalse();
    }

    [Fact]
    public async Task Upload_RejectsContentThatDoesNotMatchTheExtension()
    {
        var result = await _storage.UploadDocumentAsync(TextFile("not really a pdf", "fake.pdf"), 1, 1);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_RemovesTheFile()
    {
        await _storage.UploadDocumentAsync(TextFile("bye"), 3, 1);

        (await _storage.DeleteDocumentAsync(3, 1)).Succeeded.Should().BeTrue();
        (await _storage.DocumentExistsAsync(3, 1)).Data.Should().BeFalse();
        (await _storage.DownloadDocumentAsync(3, 1)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Copy_RejectsPathsOutsideTheStorageFolder()
    {
        var result = await _storage.CopyDocumentAsync("../../etc/passwd", "user-1");

        result.Succeeded.Should().BeFalse();
    }
}
