using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DocFlowHub.Infrastructure.Data;

/// <summary>
/// Retries transient SQL errors for ordinary queries and <c>SaveChanges</c> calls, most
/// importantly error 40613, which Azure SQL serverless returns while it resumes from
/// auto-pause (it usually needs up to a minute).
/// </summary>
/// <remarks>
/// The stock <see cref="SqlServerRetryingExecutionStrategy"/> throws as soon as code opens
/// its own transaction (<c>BeginTransactionAsync</c>), which several services do. This
/// strategy lets those calls run instead, but never retries them: the transaction is lost
/// with the connection, so a failure inside it surfaces exactly as it did before retries
/// were enabled.
/// </remarks>
public sealed class ResilientSqlServerExecutionStrategy : SqlServerRetryingExecutionStrategy
{
    // Exponential back-off capped at 30 s: 8 retries wait roughly 2 minutes in total.
    public const int DefaultMaxRetryCount = 8;
    public static readonly TimeSpan DefaultMaxRetryDelay = TimeSpan.FromSeconds(30);

    public ResilientSqlServerExecutionStrategy(ExecutionStrategyDependencies dependencies)
        : this(dependencies, DefaultMaxRetryCount, DefaultMaxRetryDelay)
    {
    }

    public ResilientSqlServerExecutionStrategy(
        ExecutionStrategyDependencies dependencies, int maxRetryCount, TimeSpan maxRetryDelay)
        : base(dependencies, maxRetryCount, maxRetryDelay, errorNumbersToAdd: null)
    {
    }

    private bool InUserTransaction =>
        Dependencies.CurrentContext.Context.Database.CurrentTransaction is not null;

    protected override void OnFirstExecution()
    {
        if (InUserTransaction)
        {
            // Skip the base check that rejects user-initiated transactions.
            ExceptionsEncountered.Clear();
            return;
        }

        base.OnFirstExecution();
    }

    protected override bool ShouldRetryOn(Exception exception) =>
        !InUserTransaction && base.ShouldRetryOn(exception);
}
