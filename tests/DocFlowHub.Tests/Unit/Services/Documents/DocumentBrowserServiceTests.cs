using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Models.Projects;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Documents;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Documents;

/// <summary>Visibility, filters, paging, tree and inspector rules (the SQLite demo tests cover translation).</summary>
public class DocumentBrowserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DocumentBrowserService _service;
    private readonly DateTime _now = DateTime.UtcNow;

    public DocumentBrowserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new DocumentBrowserService(_context, NullLogger<DocumentBrowserService>.Instance);
        Seed();
    }

    public void Dispose() => _context.Dispose();

    // me: project "Finance" (folders reports > 2025, budgets; archived "old") and a private project of
    // other's. Docs: 1 me/reports, 2 me/2025, 3 me/no project, 4 other shared with my team, 5 other
    // private, 6 me deleted.
    private void Seed()
    {
        _context.Users.AddRange(
            new ApplicationUser { Id = "me", Email = "me@x.test", UserName = "me@x.test", FirstName = "Anna", LastName = "Kowalska" },
            new ApplicationUser { Id = "other", Email = "other@x.test", UserName = "other@x.test" });

        _context.Teams.Add(new Team { Id = 1, Name = "Shared", OwnerId = "other" });
        _context.TeamMembers.AddRange(
            new TeamMember { Id = 1, TeamId = 1, UserId = "other", Role = TeamRole.Admin, JoinedAt = _now },
            new TeamMember { Id = 2, TeamId = 1, UserId = "me", Role = TeamRole.Member, JoinedAt = _now });

        _context.Projects.AddRange(
            new Project { Id = 1, Name = "Finance", OwnerId = "me", Color = "#22c55e" },
            new Project { Id = 2, Name = "Theirs", OwnerId = "other" },
            new Project { Id = 3, Name = "Closed", OwnerId = "me", IsActive = false });
        _context.Folders.AddRange(
            new Folder { Id = 1, Name = "reports", ProjectId = 1, Path = "/reports", CreatedByUserId = "me" },
            new Folder { Id = 2, Name = "2025", ProjectId = 1, ParentFolderId = 1, Path = "/reports/2025", Level = 1, CreatedByUserId = "me" },
            new Folder { Id = 3, Name = "budgets", ProjectId = 1, Path = "/budgets", CreatedByUserId = "me" },
            new Folder { Id = 4, Name = "old", ProjectId = 1, Path = "/old", IsArchived = true, CreatedByUserId = "me" });

        _context.DocumentCategories.Add(new DocumentCategory { Id = 1, Name = "Finance", IsActive = true });

        var doc1 = Doc(1, "me", "Quarterly report", ".pdf", 300, _now.AddDays(-1), project: 1, folder: 1);
        doc1.Categories.Add(_context.DocumentCategories.Local.Single());
        _context.Documents.AddRange(
            doc1,
            Doc(2, "me", "Annual plan", ".PDF", 100, _now.AddDays(-2), project: 1, folder: 2),
            Doc(3, "me", "notes", ".md", 200, _now.AddDays(-3)),
            Doc(4, "other", "Budget draft", ".docx", 50, _now.AddDays(-4), team: 1),
            Doc(5, "other", "Secret", ".txt", 10, _now),
            Doc(6, "me", "Gone", ".pdf", 10, _now, deleted: true));

        _context.DocumentVersions.AddRange(
            Version(1, 1, 1, "me", _now.AddDays(-30), "Initial upload"),
            Version(2, 1, 2, "other", _now.AddDays(-1), "Fix totals"),
            Version(3, 2, 1, "me", _now.AddDays(-2), null),
            Version(4, 3, 1, "me", _now.AddDays(-3), null),
            Version(5, 4, 1, "other", _now.AddDays(-4), null));
        _context.DocumentSummaries.Add(new DocumentSummary
        {
            Id = 1, DocumentId = 1, Summary = "Revenue grew.", AIModel = "gpt-4o-mini", ConfidenceScore = 0.9, GeneratedAt = _now,
        });
        _context.SaveChanges();
    }

    private static Document Doc(int id, string owner, string title, string type, long size, DateTime updated,
        int? project = null, int? folder = null, int? team = null, bool deleted = false) => new()
    {
        Id = id, Title = title, Description = $"About {title}", OwnerId = owner, FileType = type, FileSize = size,
        CreatedAt = updated.AddDays(-50), UpdatedAt = updated, ProjectId = project, FolderId = folder, TeamId = team,
        IsDeleted = deleted,
    };

    private static DocumentVersion Version(int id, int doc, int number, string user, DateTime at, string? message) => new()
    {
        Id = id, DocumentId = doc, VersionNumber = number, UserId = user, CreatedAt = at, FileSize = 100 * number,
        ChangeSummary = message ?? string.Empty, FileHash = "ABCDEF0123456789ABCDEF0123456789",
    };

    private async Task<DocumentListPage> ListAsync(DocumentListQuery query)
    {
        var result = await _service.GetDocumentsAsync("me", query);
        result.Succeeded.Should().BeTrue(result.Error);
        return result.Data!;
    }

    [Fact]
    public async Task Documents_ListsOwnedAndTeamSharedNewestFirst()
    {
        var page = await ListAsync(new DocumentListQuery());

        page.Items.Select(r => r.Id).Should().Equal(1, 2, 3, 4);
        page.Total.Should().Be(4);
        var first = page.Items[0];
        first.Version.Should().Be(2);
        first.HasSummary.Should().BeTrue();
        first.IsOwner.Should().BeTrue();
        first.OwnerName.Should().Be("Anna Kowalska");
        first.Location.Should().Be("Finance / reports");
        page.Items[1].Location.Should().Be("Finance / reports / 2025");
        page.Items[3].TeamName.Should().Be("Shared");
        page.Items[3].OwnerName.Should().Be("other@x.test");
        page.Items[3].IsOwner.Should().BeFalse();
    }

    [Theory]
    [InlineData("pdf", new[] { 1, 2 })]
    [InlineData(".MD", new[] { 3 })]
    public async Task Documents_FilterByTypeIgnoresCaseAndDot(string type, int[] expected)
    {
        var page = await ListAsync(new DocumentListQuery { FileType = type });
        page.Items.Select(r => r.Id).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Documents_FilterByOwnerFolderProjectTeamCategoryAndSearch()
    {
        (await ListAsync(new() { Owner = DocumentOwnerFilter.Others })).Items.Select(r => r.Id).Should().Equal(4);
        (await ListAsync(new() { Owner = DocumentOwnerFilter.Me })).Total.Should().Be(3);
        (await ListAsync(new() { ProjectId = 1 })).Items.Select(r => r.Id).Should().Equal(1, 2);
        (await ListAsync(new() { FolderId = 1 })).Items.Select(r => r.Id).Should().Equal(1);
        (await ListAsync(new() { TeamId = 1 })).Items.Select(r => r.Id).Should().Equal(4);
        (await ListAsync(new() { CategoryId = 1 })).Items.Select(r => r.Id).Should().Equal(1);
        (await ListAsync(new() { Search = "plan" })).Items.Select(r => r.Id).Should().Equal(2);
        (await ListAsync(new() { Search = "About Budget" })).Items.Select(r => r.Id).Should().Equal(4);
    }

    [Fact]
    public async Task Documents_SortsAndClampsThePage()
    {
        (await ListAsync(new() { Sort = DocumentSort.Name, Descending = false })).Items.Select(r => r.Title)
            .Should().Equal("Annual plan", "Budget draft", "notes", "Quarterly report");
        (await ListAsync(new() { Sort = DocumentSort.Size, Descending = true })).Items.Select(r => r.Id)
            .Should().Equal(1, 3, 2, 4);

        var last = await ListAsync(new() { PageSize = 3, Page = 99 });
        last.Page.Should().Be(2);
        last.Items.Select(r => r.Id).Should().Equal(4);
        (last.From, last.To, last.PageCount).Should().Be((4, 4, 2));
    }

    [Fact]
    public async Task Tree_HasOwnActiveProjectsNestedFoldersAndCounts()
    {
        var result = await _service.GetTreeAsync("me");

        result.Succeeded.Should().BeTrue(result.Error);
        var tree = result.Data!;
        tree.TotalDocuments.Should().Be(4);
        tree.StorageBytes.Should().Be(650);
        tree.Projects.Select(p => p.Name).Should().Equal("Finance");
        var finance = tree.Projects[0];
        finance.DocumentCount.Should().Be(2);
        finance.Color.Should().Be("#22c55e");
        finance.Folders.Select(f => f.Name).Should().Equal("budgets", "reports");
        var reports = finance.Folders[1];
        reports.DocumentCount.Should().Be(1);
        reports.Children.Select(f => (f.Name, f.DocumentCount)).Should().Equal(("2025", 1));
        tree.FindFolder(2)!.Value.Project.Id.Should().Be(1);
        tree.FindFolder(4).Should().BeNull();
        tree.Teams.Should().ContainSingle(t => t.Name == "Shared" && t.MemberCount == 2);
        tree.FileTypes.Select(t => (t.FileType, t.Count)).Should().Equal(("pdf", 2), ("docx", 1), ("md", 1));
    }

    [Fact]
    public async Task Inspector_ShowsSummaryAndLatestVersions()
    {
        var result = await _service.GetInspectorAsync("me", 1);

        result.Succeeded.Should().BeTrue(result.Error);
        var doc = result.Data!;
        doc.Location.Should().Be("Finance / reports");
        doc.Version.Should().Be(2);
        doc.Hash.Should().Be("abcdef0");
        doc.VersionCount.Should().Be(2);
        doc.VersionBytes.Should().Be(300);
        doc.Categories.Should().Equal("Finance");
        doc.Summary!.Text.Should().Be("Revenue grew.");
        doc.RecentVersions.Select(v => (v.Number, v.Message, v.Author)).Should().Equal(
            (2, "Fix totals", "other@x.test"),
            (1, "Initial upload", "Anna Kowalska"));
    }

    [Theory]
    [InlineData(5)] // private document of another user
    [InlineData(6)] // deleted
    [InlineData(999)]
    public async Task Inspector_FailsForDocumentsTheUserCannotSee(int id)
    {
        var result = await _service.GetInspectorAsync("me", id);
        result.Succeeded.Should().BeFalse();
    }
}
