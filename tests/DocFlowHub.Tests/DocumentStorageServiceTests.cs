using Azure.Storage.Blobs;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Infrastructure.Services.Storage;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;

namespace DocFlowHub.Tests;

public class DocumentStorageServiceTests
{
    private readonly Mock<IOptions<DocumentStorageOptions>> _optionsMock;
    private readonly Mock<ILogger<DocumentStorageService>> _loggerMock;
    private readonly DocumentStorageOptions _options;
    private readonly string _connectionString;

    public DocumentStorageServiceTests()
    {
        // Load configuration from appsettings.Development.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        // Validate configuration
        _connectionString = configuration.GetSection("Storage:ConnectionString").Value 
            ?? throw new InvalidOperationException(
                "Storage:ConnectionString is required in appsettings.Development.json for tests. " +
                "Please ensure the configuration file exists and contains a valid connection string.");
        
        _options = new DocumentStorageOptions
        {
            ConnectionString = _connectionString,
            ContainerName = "documents",
            MaxFileSizeBytes = 31_457_280,
            AllowedFileTypes = new[] { ".txt", ".pdf", ".doc", ".docx" }
        };

        _optionsMock = new Mock<IOptions<DocumentStorageOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(_options);
        
        _loggerMock = new Mock<ILogger<DocumentStorageService>>();
    }

    [Fact]
    public async Task CanConnectToStorage_ShouldReturnTrue()
    {
        // Arrange
        var service = new DocumentStorageService(_optionsMock.Object, _loggerMock.Object);

        // Act
        var result = await service.DocumentExistsAsync("test.txt");

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UploadDocument_WithValidFile_ShouldSucceed()
    {
        // Arrange
        var service = new DocumentStorageService(_optionsMock.Object, _loggerMock.Object);
        var content = "Test content";
        var fileName = "test.txt";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "test", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        // Act
        var result = await service.UploadDocumentAsync(file, fileName);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadDocument_WithInvalidFileType_ShouldFail()
    {
        // Arrange
        var service = new DocumentStorageService(_optionsMock.Object, _loggerMock.Object);
        var content = "Test content";
        var fileName = "test.exe";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "test", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/x-msdownload"
        };

        // Act
        var result = await service.UploadDocumentAsync(file, fileName);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("File type .exe is not allowed");
    }

    [Fact]
    public async Task UploadAndDownload_ShouldReturnSameContent()
    {
        // Arrange
        var service = new DocumentStorageService(_optionsMock.Object, _loggerMock.Object);
        var content = "Test content";
        var fileName = "test-upload-download.txt";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "test", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        // Act
        var uploadResult = await service.UploadDocumentAsync(file, fileName);
        uploadResult.Succeeded.Should().BeTrue();

        var downloadResult = await service.DownloadDocumentAsync(fileName);
        
        // Assert
        downloadResult.Succeeded.Should().BeTrue();
        downloadResult.Data.Should().NotBeNull();
        using var reader = new StreamReader(downloadResult.Data!);
        var downloadedContent = await reader.ReadToEndAsync();
        downloadedContent.Should().Be(content);
    }
} 