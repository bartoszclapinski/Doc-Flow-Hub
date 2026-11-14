using DocFlowHub.Core.Constants;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Dashboard;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Data;

/// <summary>
/// Boots the public demo the way the container does (SQLite + local files + demo seed) and
/// runs the queries SQLite handles differently from SQL Server: decimal sums, DateTimeOffset
/// comparisons and case-insensitive search.
/// </summary>
public sealed class SqliteDemoFixture : IAsyncLifetime
{
    private readonly string _connectionString = $"Data Source=demo-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private SqliteConnection? _keepAlive;

    public string FilesPath { get; } = Path.Combine(Path.GetTempPath(), $"docflowhub-test-{Guid.NewGuid():N}");
    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // An in-memory SQLite database lives only while a connection to it is open.
        _keepAlive = new SqliteConnection(_connectionString);
        await _keepAlive.OpenAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoMode:Enabled"] = "true",
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["DocumentStorage:Provider"] = "Local",
                ["DocumentStorage:LocalPath"] = FilesPath,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddInfrastructure(configuration);
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        Services = services.BuildServiceProvider();

        await DbInitializer.InitializeAsync(Services);
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        if (_keepAlive != null) await _keepAlive.DisposeAsync();
        if (Directory.Exists(FilesPath)) Directory.Delete(FilesPath, recursive: true);
    }
}

public class SqliteDemoTests : IClassFixture<SqliteDemoFixture>
{
    private readonly SqliteDemoFixture _fixture;

    public SqliteDemoTests(SqliteDemoFixture fixture) => _fixture = fixture;

    private T Get<T>(IServiceScope scope) where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

    private static async Task<string> UserIdAsync(IServiceScope scope, string email) =>
        (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email))!.Id;

    [Fact]
    public async Task Startup_SeedsDocumentsWithAFileForEveryVersion()
    {
        using var scope = _fixture.Services.CreateScope();
        var context = Get<ApplicationDbContext>(scope);
        var storage = Get<IDocumentStorageService>(scope);

        storage.Should().BeOfType<LocalDocumentStorageService>();
        var versions = await context.DocumentVersions.ToListAsync();
        versions.Should().NotBeEmpty();

        foreach (var version in versions)
        {
            var exists = await storage.DocumentExistsAsync(version.DocumentId, version.VersionNumber);
            exists.Data.Should().BeTrue($"document {version.DocumentId} v{version.VersionNumber} needs a file");
        }
    }

    [Fact]
    public async Task Startup_DoesNotCreateTheDefaultAdmin()
    {
        using var scope = _fixture.Services.CreateScope();
        var users = Get<UserManager<ApplicationUser>>(scope);

        (await users.FindByEmailAsync("admin@docflowhub.com")).Should().BeNull();
        (await users.FindByEmailAsync(DemoAccounts.AdminEmail)).Should().NotBeNull();
    }

    [Fact]
    public async Task DocumentSearch_IgnoresCase()
    {
        using var scope = _fixture.Services.CreateScope();
        var context = Get<ApplicationDbContext>(scope);
        var documents = Get<IDocumentService>(scope);
        var userId = await UserIdAsync(scope, DemoAccounts.UserEmail);

        var title = (await context.Documents.Where(d => d.OwnerId == userId).Select(d => d.Title).FirstAsync());
        var word = title.Split(' ')[0];

        foreach (var term in new[] { word.ToUpperInvariant(), word.ToLowerInvariant() })
        {
            var result = await documents.GetDocumentsAsync(userId, new DocumentFilter { SearchTerm = term, PageSize = 100 });
            result.Succeeded.Should().BeTrue(result.Error);
            result.Data!.Items.Should().Contain(d => d.Title == title, $"searching for '{term}' should find '{title}'");
        }
    }

    [Fact]
    public async Task CaseInsensitiveInstr_HandlesPolishLetters()
    {
        using var scope = _fixture.Services.CreateScope();
        var context = Get<ApplicationDbContext>(scope);

        var position = await context.Database
            .SqlQueryRaw<int>("SELECT instr('Zażółć gęślą jaźń', 'GĘŚLĄ') AS Value")
            .SingleAsync();

        position.Should().Be(8);
    }

    [Fact]
    public async Task AiCostStatistics_SumDecimalCosts()
    {
        using var scope = _fixture.Services.CreateScope();
        var tracking = Get<IAIUsageTrackingService>(scope);

        foreach (var email in new[] { DemoAccounts.UserEmail, DemoAccounts.AdminEmail })
        {
            var limits = await tracking.GetUserRateLimitsAsync(await UserIdAsync(scope, email));
            limits.Succeeded.Should().BeTrue(limits.Error);
        }

        var system = await tracking.GetSystemUsageStatisticsAsync();
        system.Succeeded.Should().BeTrue(system.Error);
        system.Data!.TotalCost.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UserStatistics_CompareLockoutDates()
    {
        using var scope = _fixture.Services.CreateScope();
        var statistics = await Get<IUserManagementService>(scope).GetUserStatisticsAsync();

        // The service swallows query errors and returns zeros, so check real numbers.
        statistics.TotalUsers.Should().BeGreaterThanOrEqualTo(2);
        statistics.ActiveUsers.Should().Be(statistics.TotalUsers);
    }
    [Theory]
    [InlineData(DashboardPeriod.Week)]
    [InlineData(DashboardPeriod.All)]
    public async Task Dashboard_RunsOnSqliteAndMatchesTheDocumentList(DashboardPeriod period)
    {
        using var scope = _fixture.Services.CreateScope();
        var userId = await UserIdAsync(scope, DemoAccounts.UserEmail);

        var result = await Get<IDashboardService>(scope).GetUserDashboardAsync(userId, period);

        result.Succeeded.Should().BeTrue(result.Error);
        var dashboard = result.Data!;
        var list = await Get<IDocumentService>(scope).GetDocumentsAsync(userId, new DocumentFilter { PageSize = 1 });
        dashboard.Documents.Should().Be(list.Data!.TotalItems);
        dashboard.Versions.Should().BeGreaterThanOrEqualTo(dashboard.Documents);
        dashboard.RecentDocuments.Should().NotBeEmpty();
        dashboard.RecentDocuments.Should().BeInDescendingOrder(d => d.ModifiedAt);
        dashboard.Storage.Sum(s => s.Count).Should().Be(dashboard.Documents);
        dashboard.TeamList.Should().NotBeEmpty();
        dashboard.Activity.Should().BeInDescendingOrder(a => a.At);
        (period == DashboardPeriod.All).Should().Be(dashboard.VersionsInPeriod == null);
    }

    [Fact]
    public async Task AdminOverview_RunsOnSqlite()
    {
        using var scope = _fixture.Services.CreateScope();

        var result = await Get<IDashboardService>(scope).GetAdminOverviewAsync(DashboardPeriod.Month);

        result.Succeeded.Should().BeTrue(result.Error);
        var overview = result.Data!;
        overview.Users.Should().BeGreaterThanOrEqualTo(2);
        overview.Administrators.Should().BeGreaterThanOrEqualTo(1);
        overview.RecentUsers.Should().Contain(u => u.Email == DemoAccounts.AdminEmail && u.IsAdministrator);
        overview.RecentUsers.Should().Contain(u => u.Email == DemoAccounts.UserEmail && !u.IsAdministrator);
        overview.Documents.Should().BeGreaterThan(0);
        overview.StorageBytes.Should().BeGreaterThan(0);
        overview.LargestTeams.Should().NotBeEmpty();
        overview.Activity.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DocumentBrowser_RunsOnSqliteAndMatchesTheDocumentList()
    {
        using var scope = _fixture.Services.CreateScope();
        var userId = await UserIdAsync(scope, DemoAccounts.UserEmail);
        var browser = Get<IDocumentBrowserService>(scope);

        var tree = await browser.GetTreeAsync(userId);
        tree.Succeeded.Should().BeTrue(tree.Error);
        var list = await Get<IDocumentService>(scope).GetDocumentsAsync(userId, new DocumentFilter { PageSize = 1 });
        tree.Data!.TotalDocuments.Should().Be(list.Data!.TotalItems);
        tree.Data.Projects.Should().NotBeEmpty();
        tree.Data.FileTypes.Sum(t => t.Count).Should().Be(tree.Data.TotalDocuments);

        var page = await browser.GetDocumentsAsync(userId, new DocumentListQuery { Sort = DocumentSort.Name, Descending = false });
        page.Succeeded.Should().BeTrue(page.Error);
        page.Data!.Total.Should().Be(tree.Data.TotalDocuments);
        page.Data.Items.Should().HaveCount(Math.Min(25, page.Data.Total));
        page.Data.Items.Select(r => r.Title.ToLowerInvariant()).Should().BeInAscendingOrder(StringComparer.Ordinal);

        var project = tree.Data.Projects.First(p => p.DocumentCount > 0);
        var inProject = await browser.GetDocumentsAsync(userId, new DocumentListQuery
        {
            ProjectId = project.Id, FileType = "PDF", Owner = DocumentOwnerFilter.Me, Search = "A", CategoryId = 1,
        });
        inProject.Succeeded.Should().BeTrue(inProject.Error);

        var first = page.Data.Items[0];
        var upper = await browser.GetDocumentsAsync(userId, new DocumentListQuery { Search = first.Title.ToUpperInvariant() });
        upper.Data!.Items.Should().Contain(r => r.Id == first.Id, "search ignores case on SQLite");

        var inspector = await browser.GetInspectorAsync(userId, first.Id);
        inspector.Succeeded.Should().BeTrue(inspector.Error);
        inspector.Data!.RecentVersions.Should().NotBeEmpty();
        inspector.Data.VersionCount.Should().BeGreaterThanOrEqualTo(inspector.Data.RecentVersions.Count);
    }
}
