using System;
using System.Threading.Tasks;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Projects;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Documents;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Documents;

public class DocumentMoveTests
{
    private readonly ApplicationDbContext _context;
    private readonly DocumentService _documentService;

    public DocumentMoveTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var storageMock = new Mock<IDocumentStorageService>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var loggerMock = new Mock<ILogger<DocumentService>>();

        _documentService = new DocumentService(_context, storageMock.Object, scopeFactoryMock.Object, loggerMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        // User
        var user = new ApplicationUser { Id = "owner1", UserName = "owner@example.com" };
        _context.Users.Add(user);

        // Projects
        var projectA = new Project { Id = 1, Name = "Project A", OwnerId = "owner1" };
        var projectB = new Project { Id = 2, Name = "Project B", OwnerId = "owner1" };
        _context.Projects.AddRange(projectA, projectB);

        // Folder in ProjectB
        var folderB1 = new Folder { Id = 1, Name = "Folder B1", ProjectId = 2, Path = "/Folder B1", CreatedByUserId = "owner1" };
        _context.Folders.Add(folderB1);

        // Document in ProjectA
        var doc = new Document
        {
            Id = 10,
            Title = "Doc1",
            OwnerId = "owner1",
            ProjectId = 1,
            FileType = ".txt",
            FileSize = 10,
            CreatedAt = DateTime.UtcNow
        };
        _context.Documents.Add(doc);
        _context.SaveChanges();
    }

    [Fact]
    public async Task MoveDocumentAsync_Should_Update_Project_And_Folder()
    {
        // Arrange
        var request = new MoveDocumentRequest
        {
            DocumentId = 10,
            ProjectId = 2,
            FolderId = 1,
            UserId = "owner1"
        };

        // Act
        var result = await _documentService.MoveDocumentAsync(request);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.ProjectId.Should().Be(2);
        result.Data.FolderId.Should().Be(1);
    }

    [Fact]
    public async Task MoveDocumentAsync_WithNonOwner_Should_Fail()
    {
        // Arrange
        var request = new MoveDocumentRequest
        {
            DocumentId = 10,
            ProjectId = 2,
            FolderId = 1,
            UserId = "notOwner"
        };

        // Act
        var result = await _documentService.MoveDocumentAsync(request);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Not authorized");
    }
} 