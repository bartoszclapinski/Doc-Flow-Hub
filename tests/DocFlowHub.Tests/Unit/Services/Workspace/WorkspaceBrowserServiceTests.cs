using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Projects;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Workspace;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Workspace;

/// <summary>Ownership, counts and tree order of the projects, folders and teams screens.</summary>
public class WorkspaceBrowserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly WorkspaceBrowserService _service;
    private readonly DateTime _now = DateTime.UtcNow;

    public WorkspaceBrowserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new WorkspaceBrowserService(_context, NullLogger<WorkspaceBrowserService>.Instance);
        Seed();
    }

    public void Dispose() => _context.Dispose();

    // me owns "Finance" (reports > 2025, budgets, archived "old") and archived "Closed"; other owns "Theirs".
    // Team "Shared": other is owner/admin, me is a member, "lurker" too.
    private void Seed()
    {
        _context.Users.AddRange(
            new ApplicationUser { Id = "me", Email = "me@x.test", UserName = "me@x.test", FirstName = "Anna", LastName = "Kowalska", LastLoginAt = _now },
            new ApplicationUser { Id = "other", Email = "other@x.test", UserName = "other@x.test" },
            new ApplicationUser { Id = "lurker", Email = "lurker@x.test", UserName = "lurker@x.test", FirstName = "Piotr", LastName = "Z" });

        _context.Teams.AddRange(
            new Team { Id = 1, Name = "Shared", OwnerId = "other", CreatedAt = _now.AddDays(-10) },
            new Team { Id = 2, Name = "Private club", OwnerId = "other" });
        _context.TeamMembers.AddRange(
            new TeamMember { Id = 1, TeamId = 1, UserId = "other", Role = TeamRole.Admin, JoinedAt = _now.AddDays(-10) },
            new TeamMember { Id = 2, TeamId = 1, UserId = "me", Role = TeamRole.Member, JoinedAt = _now.AddDays(-5) },
            new TeamMember { Id = 3, TeamId = 1, UserId = "lurker", Role = TeamRole.Member, JoinedAt = _now.AddDays(-1) },
            new TeamMember { Id = 4, TeamId = 2, UserId = "other", Role = TeamRole.Admin, JoinedAt = _now });

        _context.Projects.AddRange(
            new Project { Id = 1, Name = "Finance", OwnerId = "me", Description = "Money", CreatedAt = _now.AddDays(-30) },
            new Project { Id = 2, Name = "Closed", OwnerId = "me", IsActive = false },
            new Project { Id = 3, Name = "Theirs", OwnerId = "other" });
        _context.Folders.AddRange(
            new Folder { Id = 1, Name = "reports", ProjectId = 1, Path = "/reports", CreatedByUserId = "me" },
            new Folder { Id = 2, Name = "2025", ProjectId = 1, ParentFolderId = 1, Path = "/reports/2025", Level = 1, CreatedByUserId = "me" },
            new Folder { Id = 3, Name = "budgets", ProjectId = 1, Path = "/budgets", CreatedByUserId = "me" },
            new Folder { Id = 4, Name = "old", ProjectId = 1, Path = "/old", IsArchived = true, CreatedByUserId = "me" });

        _context.Documents.AddRange(
            Doc(1, "me", 1, 1, 300, _now.AddDays(-1)),
            Doc(2, "me", 1, 2, 100, _now.AddHours(-1)),
            Doc(3, "me", 1, null, 50, _now.AddDays(-3), team: 1),
            Doc(4, "other", null, null, 10, _now, team: 1),
            Doc(5, "me", 1, 1, 999, _now, deleted: true));
        _context.DocumentVersions.AddRange(
            new DocumentVersion { Id = 1, DocumentId = 1, VersionNumber = 1, UserId = "me", CreatedAt = _now.AddDays(-2) },
            new DocumentVersion { Id = 2, DocumentId = 1, VersionNumber = 2, UserId = "other", CreatedAt = _now.AddDays(-1) });
        _context.DocumentSummaries.Add(new DocumentSummary { Id = 1, DocumentId = 1, Summary = "s", GeneratedAt = _now });
        _context.SaveChanges();
    }

    private static Document Doc(int id, string owner, int? project, int? folder, long size, DateTime updated, int? team = null, bool deleted = false) => new()
    {
        Id = id, Title = $"Doc {id}", OwnerId = owner, FileType = ".pdf", FileSize = size, ProjectId = project, FolderId = folder,
        TeamId = team, CreatedAt = updated.AddDays(-5), UpdatedAt = updated, IsDeleted = deleted,
    };

    [Fact]
    public async Task Projects_ListOwnProjectsByStateWithCounts()
    {
        var active = (await _service.GetProjectsAsync("me", archived: false)).Data!;
        active.Select(p => p.Name).Should().Equal("Finance");
        var finance = active[0];
        (finance.FolderCount, finance.DocumentCount, finance.Bytes).Should().Be((4, 3, 450));
        finance.UpdatedAt.Should().BeCloseTo(_now.AddHours(-1), TimeSpan.FromSeconds(1));

        (await _service.GetProjectsAsync("me", archived: true)).Data!.Select(p => p.Name).Should().Equal("Closed");
    }

    [Fact]
    public async Task Project_HasFoldersInTreeOrderAndAiCoverage()
    {
        var project = (await _service.GetProjectAsync("me", 1)).Data!;

        project.Folders.Select(f => (f.Name, f.Level)).Should().Equal(("budgets", 0), ("old", 0), ("reports", 0), ("2025", 1));
        project.Folders.Single(f => f.Name == "old").IsArchived.Should().BeTrue();
        project.Folders.Single(f => f.Name == "reports").DocumentCount.Should().Be(1);
        project.SummarizedCount.Should().Be(1);
        project.OwnerName.Should().Be("Anna Kowalska");
        (await _service.GetProjectAsync("me", 3)).Succeeded.Should().BeFalse("projects are owner-only");
    }

    [Fact]
    public async Task FolderView_ListsSubfoldersAndDocumentsHere()
    {
        var root = (await _service.GetFolderViewAsync("me", 1, null)).Data!;
        root.Folder.Should().BeNull();
        root.Subfolders.Select(f => f.Name).Should().Equal("budgets", "reports", "old");
        root.Subfolders.Single(f => f.Name == "reports").ItemCount.Should().Be(2); // "2025" + doc 1
        root.Documents.Select(d => d.Id).Should().Equal(3);
        root.IsEmpty.Should().BeFalse();

        var reports = (await _service.GetFolderViewAsync("me", 1, 1)).Data!;
        reports.Folder!.Name.Should().Be("reports");
        reports.Path.Select(p => p.Name).Should().Equal("reports");
        reports.Documents.Single().LastChangeBy.Should().Be("other@x.test");
        reports.Documents.Single().Version.Should().Be(2);

        var sub = (await _service.GetFolderViewAsync("me", 1, 2)).Data!;
        sub.Path.Select(p => p.Name).Should().Equal("reports", "2025");
        (await _service.GetFolderViewAsync("me", 1, 4)).Data!.IsEmpty.Should().BeTrue();
        (await _service.GetFolderViewAsync("me", 3, null)).Succeeded.Should().BeFalse();
        (await _service.GetFolderViewAsync("me", 1, 999)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Teams_ShowRoleMembersAndDocuments()
    {
        var teams = (await _service.GetTeamsAsync("me")).Data!;
        teams.Select(t => (t.Name, t.Role, t.MemberCount, t.DocumentCount)).Should().Equal(("Shared", "member", 3, 2));

        var shared = (await _service.GetTeamAsync("other", 1)).Data!;
        shared.IsOwner.Should().BeTrue();
        shared.IsAdmin.Should().BeTrue();
        shared.Members.Select(m => m.UserId).Should().Equal("other", "me", "lurker");
        shared.Members.Single(m => m.UserId == "me").LastLoginAt.Should().NotBeNull();

        var mine = (await _service.GetTeamAsync("me", 1)).Data!;
        (mine.IsAdmin, mine.Role).Should().Be((false, "member"));
        mine.Members.Single(m => m.IsMe).Name.Should().Be("Anna Kowalska");
        (await _service.GetTeamAsync("me", 2)).Succeeded.Should().BeFalse();
    }
}
