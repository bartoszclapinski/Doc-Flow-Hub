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

namespace DocFlowHub.Tests.Unit.Services.Storage;

public class DocumentStorageServiceTests
{
    private readonly Mock<IOptions<DocumentStorageOptions>> _optionsMock;
    private readonly Mock<ILogger<DocumentStorageService>> _loggerMock;
    private readonly DocumentStorageOptions _options;
    private readonly string _connectionString;

    public DocumentStorageServiceTests()
    {
        // Load configuration from appsettings.Test.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build();

        // Validate configuration
        _connectionString = configuration.GetSection("Storage:ConnectionString").Value 
            ?? throw new InvalidOperationException(
                "Storage:ConnectionString is required in appsettings.Test.json for tests. " +
                "Please ensure the configuration file exists and contains a valid connection string.");
        
        var accountName = configuration.GetSection("Storage:AccountName").Value
            ?? throw new InvalidOperationException("Storage:AccountName is required in appsettings.Test.json for tests.");
            
        var accountKey = configuration.GetSection("Storage:AccountKey").Value
            ?? throw new InvalidOperationException("Storage:AccountKey is required in appsettings.Test.json for tests.");
        
        _options = new DocumentStorageOptions
        {
            ConnectionString = _connectionString,
            AccountName = accountName,
            AccountKey = accountKey,
            ContainerName = "documents-test",
            MaxFileSizeBytes = 31_457_280,
            AllowedFileTypes = new[] { ".txt", ".pdf", ".doc", ".docx" }
        };

        _optionsMock = new Mock<IOptions<DocumentStorageOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(_options);
        
        _loggerMock = new Mock<ILogger<DocumentStorageService>>();
    }

    [Fact]
    public async Task DocumentExists_WithValidParameters_ShouldReturnTrue()
    {
        // Arrange
        var service = new DocumentStorageService(_optionsMock.Object, _loggerMock.Object);
        const int documentId = 1;
        const int versionNumber = 1;

        // Act
        var result = await service.DocumentExistsAsync(documentId, versionNumber);

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
        const int documentId = 100;
        const int versionNumber = 1;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "test", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        // Act
        var result = await service.UploadDocumentAsync(file, documentId, versionNumber);

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
        const int documentId = 101;
        const int versionNumber = 1;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "test", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/x-msdownload"
        };

        // Act
        var result = await service.UploadDocumentAsync(file, documentId, versionNumber);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("File type .exe is not allowed");
    }

    [Fact]
    public async Task UploadAndDownload_ShouldReturnSameContent()
    {
        // Arrange
        var service = new DocumentStorageService(_optionsMock.Object, _loggerMock.Object);
        var content = "Test content for upload/download test";
        var fileName = "test-upload-download.txt";
        const int documentId = 102;
        const int versionNumber = 1;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "test", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        // Act
        var uploadResult = await service.UploadDocumentAsync(file, documentId, versionNumber);
        uploadResult.Succeeded.Should().BeTrue();

        var downloadResult = await service.DownloadDocumentAsync(documentId, versionNumber);
        
        // Assert
        downloadResult.Succeeded.Should().BeTrue();
        downloadResult.Data.Should().NotBeNull();
        var downloadedContent = Encoding.UTF8.GetString(downloadResult.Data!);
        downloadedContent.Should().Be(content);
    }

    [Fact]
    public async Task DeleteDocument_WithExistingDocument_ShouldSucceed()
    {
        // Arrange
        var service = new DocumentStorageService(_optionsMock.Object, _loggerMock.Object);
        var content = "Test content for deletion";
        var fileName = "test-delete.txt";
        const int documentId = 103;
        const int versionNumber = 1;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "test", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        // Upload first
        var uploadResult = await service.UploadDocumentAsync(file, documentId, versionNumber);
        uploadResult.Succeeded.Should().BeTrue();

        // Act - Delete
        var deleteResult = await service.DeleteDocumentAsync(documentId, versionNumber);

        // Assert
        deleteResult.Succeeded.Should().BeTrue();
        
        // Verify document no longer exists
        var existsResult = await service.DocumentExistsAsync(documentId, versionNumber);
        existsResult.Succeeded.Should().BeTrue();
        existsResult.Data.Should().BeFalse();
    }
} 