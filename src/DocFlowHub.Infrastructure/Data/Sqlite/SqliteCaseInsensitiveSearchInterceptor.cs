using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DocFlowHub.Infrastructure.Data.Sqlite;

/// <summary>
/// Makes text search case-insensitive on SQLite, as it is on SQL Server.
/// </summary>
/// <remarks>
/// EF Core translates <c>string.Contains</c> to SQLite's <c>instr()</c>, which is
/// case-sensitive and ignores column collations. Every opened connection gets its own
/// <c>instr()</c> that ignores case (Polish letters included); it replaces the built-in one.
/// </remarks>
public sealed class SqliteCaseInsensitiveSearchInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) =>
        Register(connection);

    public override Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Register(connection);
        return Task.CompletedTask;
    }

    private static void Register(DbConnection connection)
    {
        if (connection is SqliteConnection sqlite)
        {
            sqlite.CreateFunction<string?, string?, int?>(
                "instr",
                (haystack, needle) => haystack is null || needle is null
                    ? null
                    : haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) + 1,
                isDeterministic: true);
        }
    }
}
