using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Services.Teams;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DocFlowHub.Core.Identity;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using DocFlowHub.Core.Models;

namespace DocFlowHub.Tests.Unit.Services.Teams;

public class TeamServiceTests : IDisposable
{
    private readonly Mock<ILogger<TeamService>> _loggerMock;
    private readonly TeamService _teamService;
    private readonly ApplicationDbContext _context;

    public TeamServiceTests()
    {
        // Create InMemory database (same pattern as DocumentServiceTests)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        
        _loggerMock = new Mock<ILogger<TeamService>>();
        
        // Create actual context for Entity Framework operations
        _context = new ApplicationDbContext(options);
        
        // Initialize TeamService with real context and mocked logger
        _teamService = new TeamService(
            _context,
            _loggerMock.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test users (CRITICAL for navigation properties)
        var testUser1 = new ApplicationUser
        {
            Id = "user123",
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        var testUser2 = new ApplicationUser
        {
            Id = "user456",
            UserName = "testuser2@example.com",
            Email = "testuser2@example.com",
            FirstName = "Test2",
            LastName = "User2",
            EmailConfirmed = true
        };

        _context.Users.Add(testUser1);
        _context.Users.Add(testUser2);

        // Create test team with members
        var testTeam = new Team
        {
            Id = 1,
            Name = "Test Team",
            Description = "Test team description",
            OwnerId = "user123",
            CreatedAt = DateTime.UtcNow,
            Members = new List<TeamMember>
            {
                new TeamMember
                {
                    UserId = "user123",
                    Role = TeamRole.Admin,
                    JoinedAt = DateTime.UtcNow
                }
            }
        };

        _context.Teams.Add(testTeam);
        _context.SaveChanges();
    }

    // Dispose method for cleanup
    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task CreateTeamAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var request = new CreateTeamRequest
        {
            Name = "New Test Team",
            Description = "New test team description"
        };
        var ownerId = "user456"; // Use existing test user

        // Act
        var result = await _teamService.CreateTeamAsync(request, ownerId);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Name.Should().Be("New Test Team");
        result.Data.Description.Should().Be("New test team description");
        result.Data.OwnerId.Should().Be(ownerId);
    }



    [Fact]
    public async Task AddMemberAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var teamId = 1;
        var userId = "user456";
        var addedByUserId = "user123";

        // Act
        var result = await _teamService.AddMemberToTeamAsync(teamId, userId, addedByUserId);

        // Assert
        result.Succeeded.Should().BeTrue();
    }







    [Fact]
    public async Task GetUserTeamsAsync_WithValidUser_ShouldReturnTeams()
    {
        // Arrange
        var userId = "user123";
        var filter = new TeamFilter
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _teamService.GetUserTeamsAsync(userId, filter);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTeamMembersAsync_WithValidTeam_ShouldReturnMembers()
    {
        // Arrange
        var teamId = 1;

        // Act
        var result = await _teamService.GetTeamMembersAsync(teamId);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTeamAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var teamId = 1;
        var name = "Updated Team Name";
        var description = "Updated description";
        var userId = "user123";

        // Act
        var result = await _teamService.UpdateTeamAsync(teamId, name, description, userId);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Name.Should().Be("Updated Team Name");
        result.Data.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteTeamAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var teamId = 1;
        var userId = "user123"; // team owner

        // Act
        var result = await _teamService.DeleteTeamAsync(teamId, userId);

        // Assert
        result.Succeeded.Should().BeTrue();
    }


} 