using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DocFlowHub.Infrastructure.Data.Sqlite;

/// <summary>
/// Adapts the SQL Server-oriented model so it also runs on SQLite, which the public demo
/// uses (one self-contained container, rebuilt from the seed on every start). Applied only
/// when the context runs on SQLite; the SQL Server model and its migrations are unchanged.
/// </summary>
internal static class SqliteModelAdjustments
{
    public static void Apply(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                // SQL Server type names such as nvarchar(max) are not valid SQLite syntax.
                property.SetColumnType(null);

                if (string.Equals(property.GetDefaultValueSql(), "GETUTCDATE()", StringComparison.OrdinalIgnoreCase))
                {
                    property.SetDefaultValueSql("CURRENT_TIMESTAMP");
                }

                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                // SQLite can't SUM, AVG or ORDER BY decimal columns (the AI cost statistics do);
                // store them as REAL instead. Precision is ample for cost figures.
                if (clrType == typeof(decimal))
                {
                    property.SetProviderClrType(typeof(double));
                }

                // SQLite can't compare or sort DateTimeOffset (e.g. Identity's LockoutEnd, used by
                // the admin user statistics); store it as a sortable number instead.
                if (clrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(new DateTimeOffsetToBinaryConverter());
                }

                // Match SQL Server's case-insensitive comparisons and sorting.
                if (clrType == typeof(string))
                {
                    property.SetCollation("NOCASE");
                }
            }
        }
    }
}
