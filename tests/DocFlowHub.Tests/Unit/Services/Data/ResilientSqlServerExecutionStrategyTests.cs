using DocFlowHub.Infrastructure;
using DocFlowHub.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Data;

/// <summary>
/// The demo database (Azure SQL free tier) pauses when idle and rejects the first
/// connection while it wakes up. These tests check that such transient failures are
/// retried and that everything else still fails immediately. No database is needed:
/// the operations run through the strategy without opening a connection.
/// </summary>
public class ResilientSqlServerExecutionStrategyTests
{
    private static ApplicationDbContext CreateContext(int maxRetryCount) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=unused;Database=unused",
                sql => sql.ExecutionStrategy(dependencies =>
                    new ResilientSqlServerExecutionStrategy(dependencies, maxRetryCount, TimeSpan.FromMilliseconds(1))))
            .Options);

    [Fact]
    public async Task TransientError_IsRetriedUntilTheOperationSucceeds()
    {
        await using var context = CreateContext(maxRetryCount: 5);
        var attempts = 0;

        var result = await context.Database.CreateExecutionStrategy().ExecuteAsync(() =>
        {
            attempts++;
            if (attempts < 3)
                throw new TimeoutException("database is resuming");
            return Task.FromResult(42);
        });

        result.Should().Be(42);
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task TransientError_GivesUpAfterMaxRetries()
    {
        await using var context = CreateContext(maxRetryCount: 2);
        var attempts = 0;

        Func<Task> operation = () =>
        {
            attempts++;
            throw new TimeoutException("database is still paused");
        };
        var act = () => context.Database.CreateExecutionStrategy().ExecuteAsync(operation);

        await act.Should().ThrowAsync<RetryLimitExceededException>();
        attempts.Should().Be(3); // first try + 2 retries
    }

    [Fact]
    public async Task NonTransientError_IsNotRetried()
    {
        await using var context = CreateContext(maxRetryCount: 5);
        var attempts = 0;

        Func<Task> operation = () =>
        {
            attempts++;
            throw new InvalidOperationException("a bug, not a network problem");
        };
        var act = () => context.Database.CreateExecutionStrategy().ExecuteAsync(operation);

        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public void AddInfrastructure_RegistersTheResilientStrategy()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Database.CreateExecutionStrategy().Should().BeOfType<ResilientSqlServerExecutionStrategy>();
    }
}
