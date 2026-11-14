using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Admin;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Admin;

/// <summary>Counts and aggregates of the redesigned admin screens.</summary>
public class AdminBrowserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AdminBrowserService _service;
    private readonly DateTime _now = DateTime.UtcNow;

    public AdminBrowserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new AdminBrowserService(_context, NullLogger<AdminBrowserService>.Instance);
        Seed();
    }

    public void Dispose() => _context.Dispose();

    private void Seed()
    {
        _context.Users.AddRange(
            new ApplicationUser { Id = "admin", Email = "admin@x.test", FirstName = "Ada", LastName = "Admin", LastLoginAt = _now, CreatedAt = _now.AddDays(-100), EmailConfirmed = true },
            new ApplicationUser { Id = "anna", Email = "anna@x.test", FirstName = "Anna", LastName = "K", LastLoginAt = _now.AddDays(-2), CreatedAt = _now.AddDays(-3), EmailConfirmed = true },
            new ApplicationUser { Id = "off", Email = "off@x.test", IsActive = false, CreatedAt = _now.AddDays(-50), EmailConfirmed = true },
            new ApplicationUser { Id = "locked", Email = "locked@x.test", LockoutEnd = DateTimeOffset.UtcNow.AddDays(1), CreatedAt = _now.AddDays(-50) });
        _context.Roles.Add(new IdentityRole { Id = "r1", Name = "Administrator" });
        _context.UserRoles.Add(new IdentityUserRole<string> { UserId = "admin", RoleId = "r1" });
        _context.Teams.Add(new Team { Id = 1, Name = "Finance", OwnerId = "admin" });
        _context.TeamMembers.Add(new TeamMember { Id = 1, TeamId = 1, UserId = "anna", Role = TeamRole.Member });

        _context.Documents.AddRange(
            new Document { Id = 1, Title = "a", OwnerId = "anna", FileSize = 300, CreatedAt = _now },
            new Document { Id = 2, Title = "b", OwnerId = "anna", FileSize = 200, CreatedAt = _now },
            new Document { Id = 3, Title = "c", OwnerId = "admin", FileSize = 50, CreatedAt = _now });
        _context.DocumentVersions.AddRange(
            new DocumentVersion { Id = 1, DocumentId = 1, VersionNumber = 1, UserId = "anna", CreatedAt = _now.AddDays(-1) },
            new DocumentVersion { Id = 2, DocumentId = 2, VersionNumber = 1, UserId = "anna", CreatedAt = _now },
            new DocumentVersion { Id = 3, DocumentId = 3, VersionNumber = 1, UserId = "admin", CreatedAt = _now.AddDays(-40) });

        _context.AIUsageLogs.AddRange(
            Log(1, "anna", "Summarization", 0.02m, true, false, 1),
            Log(2, "anna", "Summarization", 0.02m, true, true, 2),
            Log(3, "anna", "VersionComparison", 0.05m, false, false, 10),
            Log(4, "admin", "Summarization", 0.01m, true, false, 3, daysAgo: 60));
        _context.UserActivityLogs.Add(new UserActivityLog
        {
            Id = 1, UserId = "admin", ActivityType = "UserDeactivated", Description = "Deactivated user off@x.test",
            ResourceId = "off", Timestamp = _now.AddHours(-1),
        });
        _context.SaveChanges();
    }

    private AIUsageLog Log(int id, string user, string op, decimal cost, bool ok, bool cached, int seconds, int daysAgo = 0) => new()
    {
        Id = id, UserId = user, OperationType = op, AIModel = "claude-haiku-4-5", EstimatedCost = cost, IsSuccess = ok,
        ServedFromCache = cached, ResponseTime = TimeSpan.FromSeconds(seconds), CreatedAt = _now.AddDays(-daysAgo),
    };

    [Fact]
    public async Task NavStats_CountUsersRolesActivityLockoutsAndSignups()
    {
        var stats = (await _service.GetNavStatsAsync()).Data!;
        (stats.Users, stats.Roles, stats.ActiveIn30Days, stats.Locked, stats.SignupsIn7Days).Should().Be((4, 1, 2, 1, 1));
    }

    [Fact]
    public async Task UserRows_HaveRolesTeamsStorageAiAndStatus()
    {
        var rows = (await _service.GetUserRowsAsync(new[] { "anna", "admin", "off", "locked" })).Data!;

        rows["anna"].Teams.Should().Equal("Finance");
        (rows["anna"].StorageBytes, rows["anna"].DocumentCount, rows["anna"].AiRequests).Should().Be((500, 2, 3));
        rows["admin"].IsAdministrator.Should().BeTrue();
        rows["admin"].AiRequests.Should().Be(0, "the admin's only request is older than 30 days");
        rows["off"].Status.Should().Be("active", "IsActive is not set or checked anywhere; locking is the only block");
        rows["locked"].Status.Should().Be("locked");
        rows["anna"].Status.Should().Be("active");
    }

    [Fact]
    public async Task UserDetails_IncludeActionsAboutTheUser()
    {
        var off = (await _service.GetUserDetailsAsync("off")).Data!;
        off.RecentActions.Should().ContainSingle(a => a.Type == "UserDeactivated" && a.ActorName == "Ada Admin");
        (await _service.GetUserDetailsAsync("nobody")).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Analytics_SummariseUploadsAiAndStorage()
    {
        var a = (await _service.GetAnalyticsAsync(7)).Data!;

        a.Days.Should().Be(7);
        a.UploadsPerDay.Should().HaveCount(7);
        a.UploadsPerDay.Sum(d => d.Count).Should().Be(2);
        a.UploadsPerDay[^1].Count.Should().Be(1, "one version was uploaded today");
        a.AiRequests.Should().Be(3);
        a.AiCost.Should().Be(0.09m);
        a.AiByOperation.Select(o => (o.Operation, o.Requests)).Should().Equal(("Summarization", 2), ("VersionComparison", 1));
        a.CacheHitRate.Should().BeApproximately(1 / 3.0, 0.001);
        a.FailureRate.Should().BeApproximately(1 / 3.0, 0.001);
        a.P95LatencySeconds.Should().Be(10);
        a.TopStorage.Select(s => (s.Name, s.Bytes)).Should().Equal(("Anna K", 500L), ("Ada Admin", 50L));
        a.AdminActions.Should().ContainSingle();
        (await _service.GetAnalyticsAsync(12)).Data!.Days.Should().Be(30, "unknown periods fall back to 30 days");
    }

    [Fact]
    public async Task UserAiUsage_CountsTodayTheMonthAndFourteenDays()
    {
        var usage = (await _service.GetUserAiUsageAsync("anna")).Data!;
        usage.RequestsToday.Should().Be(3);
        usage.Last14Days.Should().HaveCount(14);
        usage.Last14Days[^1].Count.Should().Be(3);
    }
}
