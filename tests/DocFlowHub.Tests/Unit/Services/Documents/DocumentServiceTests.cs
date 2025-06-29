using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Documents;
using DocFlowHub.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Documents;



public class DocumentServiceTests
{
    private readonly Mock<ApplicationDbContext> _contextMock;
    private readonly Mock<IDocumentStorageService> _storageServiceMock;
    private readonly Mock<IDocumentSummaryService> _summaryServiceMock;
    private readonly Mock<ILogger<DocumentService>> _loggerMock;
    private readonly DocumentService _documentService;
    private readonly ApplicationDbContext _context;

    public DocumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        
        _contextMock = new Mock<ApplicationDbContext>(options);
        _storageServiceMock = new Mock<IDocumentStorageService>();
        _summaryServiceMock = new Mock<IDocumentSummaryService>();
        _loggerMock = new Mock<ILogger<DocumentService>>();
        
        // Create actual context for Entity Framework operations
        _context = new ApplicationDbContext(options);
        
        _documentService = new DocumentService(
            _context,
            _storageServiceMock.Object,
            _summaryServiceMock.Object,
            _loggerMock.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test user
        var testUser = new ApplicationUser
        {
            Id = "user123",
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        _context.Users.Add(testUser);

        var testDocument = new Document
        {
            Id = 1,
            Title = "Test Document",
            Description = "Test description",
            OwnerId = "user123",
            FileType = ".txt",
            FileSize = 1024,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.Documents.Add(testDocument);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateDocumentAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            Title = "Test Document",
            Description = "Test description",
            CategoryIds = new List<int> { 1, 2 },
            OwnerId = "user123"
        };

        _storageServiceMock
            .Setup(x => x.UploadDocumentAsync(It.IsAny<IFormFile>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(ServiceResult<string>.Success("file-path"));

        // Act
        var testFile = TestDataBuilder.Documents.File().Build();
        var result = await _documentService.CreateDocumentAsync(request, testFile);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Title.Should().Be("Test Document");
        
        // Verify storage service was called
        _storageServiceMock.Verify(x => x.UploadDocumentAsync(It.IsAny<IFormFile>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task CreateDocumentAsync_WithInvalidFile_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            Title = "Test Document",
            Description = "Test description",
            OwnerId = "user123"
        };

        _storageServiceMock
            .Setup(x => x.UploadDocumentAsync(It.IsAny<IFormFile>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(ServiceResult<string>.Failure("Invalid file type"));

        // Act
        var testFile = TestDataBuilder.Documents.File().AsInvalidType().Build();
        var result = await _documentService.CreateDocumentAsync(request, testFile);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Invalid file type");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateDocumentAsync_WithInvalidTitle_ShouldReturnFailure(string invalidTitle)
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            Title = invalidTitle,
            Description = "Test description",
            OwnerId = "user123"
        };

        // Act
        var testFile = TestDataBuilder.Documents.File().Build();
        var result = await _documentService.CreateDocumentAsync(request, testFile);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Title is required");
    }

    [Fact]
    public async Task GetDocumentByIdAsync_WithExistingId_ShouldReturnDocument()
    {
        // Arrange - create a document directly in the test
        var document = new Document
        {
            Title = "Test Document for Get",
            Description = "Test description for get",
            OwnerId = "user123",
            FileType = ".txt",
            FileSize = 1024,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            Versions = new List<DocumentVersion>(),
            Categories = new List<DocumentCategory>()
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();



        // Act
        var result = await _documentService.GetDocumentByIdAsync(document.Id);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(document.Id);
        result.Data.Title.Should().Be("Test Document for Get");
    }

    [Fact]
    public async Task GetDocumentByIdAsync_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var documentId = 999;

        // Mock repository to return null
        // _documentRepositoryMock.Setup(x => x.GetByIdAsync(documentId))
        //     .ReturnsAsync((Document)null);

        // Act
        var result = await _documentService.GetDocumentByIdAsync(documentId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Document not found");
    }

    [Fact]
    public async Task UpdateDocumentAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var documentId = 1;
        var request = new UpdateDocumentRequest
        {
            Title = "Updated Title",
            Description = "Updated description"
        };

        // Act
        var result = await _documentService.UpdateDocumentAsync(documentId, request);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Title.Should().Be("Updated Title");
        result.Data.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithExistingDocument_ShouldReturnSuccess()
    {
        // Arrange
        var documentId = 1; // Use existing test document

        // Act
        var result = await _documentService.DeleteDocumentAsync(documentId);

        // Assert
        result.Succeeded.Should().BeTrue();
        
        // Verify document is marked as deleted (soft delete)
        var document = await _context.Documents.FindAsync(documentId);
        document.Should().NotBeNull();
        document.IsDeleted.Should().BeTrue();
        
        // DO NOT expect storage service call - this is soft delete only
        // Storage service is not called for soft delete operations
        _storageServiceMock.Verify(x => x.DeleteDocumentAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetDocumentsForUserAsync_WithFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var filter = new DocumentFilter
        {
            SearchTerm = "test",
            PageNumber = 1,
            PageSize = 10
        };
        var userId = "user123";

        // Act
        var result = await _documentService.GetDocumentsForUserAsync(userId, filter);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Items.Should().NotBeNull();
    }
} 