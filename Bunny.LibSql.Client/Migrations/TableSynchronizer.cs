using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunny.LibSql.Client.Attributes;
using Bunny.LibSql.Client.Migrations.InternalModels;
using Bunny.LibSql.Client.SQL;
using Bunny.LibSql.Client.TypeHandling;

namespace Bunny.LibSql.Client.Migrations;

public static class TableSynchronizer
{
    /// <summary>
    /// Generates all needed DDL to bring the type into sync:
    /// - create/drop columns
    /// - create/drop indexes for [Index] and [Join]
    /// </summary>
    public static List<string> GenerateSqlCommands(Type type,
        IEnumerable<SqliteTableInfo> existingColumns,
        IEnumerable<SqliteMasterInfo> existingIndexes
    )
    {
        var tableName = type.Name;
        var props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.PropertyType.IsLibSqlSupportedType())
            .ToArray();

        var sql = new List<string>();

        // map existing columns by name (ignore case)
        var existingColsByName = (existingColumns ?? Enumerable.Empty<SqliteTableInfo>())
            .ToDictionary(c => c.name, c => c, StringComparer.OrdinalIgnoreCase);

        // 0. Detect any type changes
        var changedProps = props
            .Where(p => existingColsByName.TryGetValue(p.Name, out var colInfo)
                        && !string.Equals(
                            SqliteToNativeTypeMap.ToSqlType(p.PropertyType) + GetNullabilityDefinition(p),
                            colInfo.type,
                            StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (changedProps.Any())
        {
            // We need to rebuild the table
            var newColumnsDef = props
                .Select(p => $"{p.Name} {SqliteToNativeTypeMap.ToSqlType(p.PropertyType)}{GetNullabilityDefinition(p)}")
                .ToList();

            var columnList = string.Join(", ", props.Select(p => p.Name));
            var columnListWithType = string.Join(", ", newColumnsDef);

            sql.Add("PRAGMA foreign_keys=OFF;");
            sql.Add("BEGIN TRANSACTION;");

            // 1) Create new shadow table
            sql.Add($"CREATE TABLE {tableName}_new ({columnListWithType});");

            // 2) Copy data across (SQLite will convert types where it can)
            sql.Add(
                $"INSERT INTO {tableName}_new ({columnList}) " +
                $"SELECT {columnList} FROM {tableName};"
            );

            // 3) Drop old and rename new
            sql.Add($"DROP TABLE {tableName};");
            sql.Add($"ALTER TABLE {tableName}_new RENAME TO {tableName};");

            sql.Add("COMMIT;");
            sql.Add("PRAGMA foreign_keys=ON;");

            // rebuild indexes as well if needed, so let the index logic run below
        }
        else
        {
            // — Columns sync: only create/add/drop if no type changes —

            var existingNames = existingColsByName.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 1. CREATE TABLE if empty
            if (existingNames.Count == 0)
            {
                var cols = props
                    .Select(p => $"{p.Name} {SqliteToNativeTypeMap.ToSqlType(p.PropertyType)}{GetNullabilityDefinition(p)}");
                sql.Add($"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", cols)});");
            }
            else
            {
                // 2. ADD missing
                foreach (var p in props)
                {
                    if (!existingNames.Contains(p.Name))
                    {
                        var typeSql = SqliteToNativeTypeMap.ToSqlType(p.PropertyType);
                        sql.Add($"ALTER TABLE {tableName} ADD COLUMN {p.Name} {typeSql}{GetNullabilityDefinition(p)};");
                    }
                }

                // 3. DROP removed (SQLite 3.35+)
                var propNames = props.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var col in existingColumns)
                {
                    if (!propNames.Contains(col.name))
                        sql.Add($"ALTER TABLE {tableName} DROP COLUMN {col.name};");
                }
            }
        }

        // — Indexes sync (unchanged) —
        var existingIdx = (existingIndexes ?? Enumerable.Empty<SqliteMasterInfo>())
            .Where(i =>
                i.type.Equals("index", StringComparison.OrdinalIgnoreCase) &&
                i.tbl_name.Equals(tableName, StringComparison.OrdinalIgnoreCase))
            .Select(i => i.name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var desired = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // [Index] attributes
        foreach (var p in props)
        {
            var att = p.GetCustomAttribute<IndexAttribute>();
            if (att != null)
            {
                var idxName = att.Name ?? $"idx_{tableName}_{p.Name}";
                var unique = att.Unique ? "UNIQUE " : "";
                var ddl = $"CREATE {unique}INDEX IF NOT EXISTS {idxName} ON {tableName}({p.Name});";
                desired[idxName] = ddl;
            }
        }

        // [Join] attributes
        foreach (var p in props)
        {
            var att = p.GetCustomAttribute<JoinAttribute>();
            if (att != null
                && typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                && p.PropertyType.IsGenericType)
            {
                var child = p.PropertyType.GetGenericArguments()[0].Name;
                var fk = att.ForeignKey;
                var idxName = $"idx_{child}_{fk}";
                var ddl = $"CREATE INDEX IF NOT EXISTS {idxName} ON {child}({fk});";
                desired[idxName] = ddl;
            }
        }

        // Add missing indexes
        foreach (var kv in desired)
            if (!existingIdx.Contains(kv.Key))
                sql.Add(kv.Value);

        // Drop stale indexes
        foreach (var idx in existingIdx)
            if (!desired.ContainsKey(idx))
                sql.Add($"DROP INDEX IF EXISTS {idx};");

        return sql;
    }

    /// <summary>
    /// Determines the nullability clause for a property based on its type and nullable attributes.
    /// </summary>
    private static string GetNullabilityDefinition(PropertyInfo p)
    {
        // Override with explicit attributes
        if (p.GetCustomAttribute<NotNullAttribute>() != null)
            return " NOT NULL";
        if (p.GetCustomAttribute<AllowNullAttribute>() != null)
            return string.Empty;

        // Value types: non-nullable by default unless it's Nullable<T>
        if (p.PropertyType.IsValueType)
        {
            return p.PropertyType.IsNullableType() ? string.Empty : " NOT NULL";
        }

        // Reference types: nullable by default
        return string.Empty;
    }
}
