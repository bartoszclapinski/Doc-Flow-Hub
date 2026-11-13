using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Dashboard;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Dashboard;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Dashboard;

/// <summary>Visibility and counting rules of the dashboard (the SQLite demo tests cover translation).</summary>
public class DashboardServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DashboardService _service;
    private readonly DateTime _now = DateTime.UtcNow;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new DashboardService(_context, NullLogger<DashboardService>.Instance);
        Seed();
    }

    public void Dispose() => _context.Dispose();

    // me: owns doc 1 (2 versions, summary) and is a member of "Shared"; other: owns doc 2 shared with
    // "Shared", doc 3 private, doc 4 deleted in "Shared"; "Mine" is a team I own without a membership row.
    private void Seed()
    {
        _context.Users.AddRange(
            new ApplicationUser { Id = "me", Email = "me@x.test", UserName = "me@x.test", FirstName = "Anna", LastName = "Kowalska" },
            new ApplicationUser { Id = "other", Email = "other@x.test", UserName = "other@x.test", FirstName = "Ewa", LastName = "Nowak" });

        var shared = new Team { Id = 1, Name = "Shared", OwnerId = "other" };
        var mine = new Team { Id = 2, Name = "Mine", OwnerId = "me" };
        _context.Teams.AddRange(shared, mine);
        _context.TeamMembers.AddRange(
            new TeamMember { Id = 1, TeamId = 1, UserId = "other", Role = TeamRole.Admin, JoinedAt = _now.AddDays(-100) },
            new TeamMember { Id = 2, TeamId = 1, UserId = "me", Role = TeamRole.Member, JoinedAt = _now.AddDays(-2) });

        _context.Documents.AddRange(
            Doc(1, "me", null, ".pdf", 100, _now.AddDays(-1)),
            Doc(2, "other", 1, ".PDF", 50, _now.AddDays(-3)),
            Doc(3, "other", null, ".docx", 999, _now),
            Doc(4, "other", 1, ".txt", 10, _now, deleted: true));
        _context.DocumentVersions.AddRange(
            Version(1, 1, 1, "me", _now.AddDays(-40)),
            Version(2, 1, 2, "me", _now.AddDays(-1)),
            Version(3, 2, 1, "other", _now.AddDays(-3)),
            Version(4, 3, 1, "other", _now),
            Version(5, 4, 1, "other", _now));
        _context.DocumentSummaries.Add(new DocumentSummary { Id = 1, DocumentId = 1, GeneratedAt = _now.AddHours(-20) });
        _context.SaveChanges();
    }

    private static Document Doc(int id, string owner, int? team, string type, long size, DateTime updated, bool deleted = false) => new()
    {
        Id = id, Title = $"Doc {id}", OwnerId = owner, TeamId = team, FileType = type, FileSize = size,
        CreatedAt = updated.AddDays(-50), UpdatedAt = updated, IsDeleted = deleted,
    };

    private static DocumentVersion Version(int id, int doc, int number, string user, DateTime at) => new()
    {
        Id = id, DocumentId = doc, VersionNumber = number, UserId = user, CreatedAt = at,
        FileHash = "ABCDEF0123456789ABCDEF0123456789",
    };

    [Fact]
    public async Task UserDashboard_CountsOnlyOwnedAndTeamSharedDocuments()
    {
        var result = await _service.GetUserDashboardAsync("me", DashboardPeriod.Week);

        result.Succeeded.Should().BeTrue(result.Error);
        var d = result.Data!;
        d.Documents.Should().Be(2);                 // 1 (own) + 2 (shared); 3 is private, 4 is deleted
        d.Versions.Should().Be(3);
        d.SharedWithMe.Should().Be(1);
        d.VersionsInPeriod.Should().Be(2);          // v2 of doc 1 and v1 of doc 2
        d.RecentDocuments.Select(r => r.Id).Should().Equal(1, 2);
        d.RecentDocuments[0].HasSummary.Should().BeTrue();
        d.RecentDocuments[0].Version.Should().Be(2);
        d.RecentDocuments[1].OwnerName.Should().Be("Ewa Nowak");
        d.DocumentsWithoutSummary.Should().Be(1);
    }

    [Fact]
    public async Task UserDashboard_MergesFileTypesCaseInsensitively()
    {
        var d = (await _service.GetUserDashboardAsync("me", DashboardPeriod.All)).Data!;

        d.Storage.Should().ContainSingle();
        d.Storage[0].FileType.Should().Be("pdf");
        d.Storage[0].Count.Should().Be(2);
        d.StorageBytes.Should().Be(150);
    }

    [Fact]
    public async Task UserDashboard_ListsTeamsWithTheUsersRole()
    {
        var d = (await _service.GetUserDashboardAsync("me", DashboardPeriod.Week)).Data!;

        d.Teams.Should().Be(2);
        d.TeamList.Should().BeEquivalentTo(new[]
        {
            new DashboardTeamRow { Id = 2, Name = "Mine", Role = "owner", MemberCount = 0 },
            new DashboardTeamRow { Id = 1, Name = "Shared", Role = "member", MemberCount = 2 },
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task UserDashboard_ActivityHasVersionsAiAndJoinsInThePeriod()
    {
        var d = (await _service.GetUserDashboardAsync("me", DashboardPeriod.Week)).Data!;

        d.Activity.Select(a => a.Kind).Should().BeEquivalentTo(new[]
        {
            ActivityKind.Version,     // doc 1 v2, 1 day ago
            ActivityKind.AiSummary,   // 20 hours ago
            ActivityKind.TeamJoined,  // me joined Shared, 2 days ago
            ActivityKind.Created,     // doc 2 v1, 3 days ago
        });
        d.Activity.Should().BeInDescendingOrder(a => a.At);
        var upload = d.Activity.Single(a => a.Kind == ActivityKind.Version);
        upload.Actor.Should().Be("Anna Kowalska");
        upload.Verb.Should().Be("uploaded v2 of");
        upload.Hash.Should().Be("abcdef0");
        d.Activity.Should().NotContain(a => a.DocumentId == 3 || a.DocumentId == 4);
    }

    [Fact]
    public async Task AdminOverview_CountsEverythingButDeletedDocuments()
    {
        var result = await _service.GetAdminOverviewAsync(DashboardPeriod.All);

        result.Succeeded.Should().BeTrue(result.Error);
        var o = result.Data!;
        o.Users.Should().Be(2);
        o.Documents.Should().Be(3);
        o.Versions.Should().Be(4);
        o.Teams.Should().Be(2);
        o.StorageBytes.Should().Be(1149);
        o.NewDocumentsInPeriod.Should().BeNull();
        o.LargestTeams[0].Name.Should().Be("Shared");
        o.Activity.Should().Contain(a => a.Kind == ActivityKind.UserRegistered);
    }
}
