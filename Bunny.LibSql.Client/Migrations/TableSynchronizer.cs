using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    /// - apply UNIQUE constraints for [Index(Unique = true)] attributes
    /// </summary>
    public static List<string> GenerateSqlCommands(Type type,
        IEnumerable<SqliteTableInfo> existingColumns,
        IEnumerable<SqliteMasterInfo> existingIndexes
    )
    {
        var tableName = type.Name;
        var tableAttr = type.GetCustomAttribute<TableAttribute>();
        if (tableAttr != null)
        {
            tableName = tableAttr.Name ?? tableName;
        }
        
        var props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.PropertyType.IsLibSqlSupportedType())
            .ToArray();

        var sql = new List<string>();

        // map existing columns by name (ignore case)
        var existingColsByName = (existingColumns ?? [])
            .ToDictionary(c => c.name, c => c, StringComparer.OrdinalIgnoreCase);

        // 0. Detect any type or nullability/uniqueness changes
        var changedProps = props
            .Where(p => existingColsByName.TryGetValue(p.Name, out var colInfo)
                        && !string.Equals(
                            BuildColumnDefinition(p),
                            colInfo.type,
                            StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (changedProps.Any())
        {
            // We need to rebuild the table
            var newColumnsDef = props
                .Select(p => BuildColumnDefinition(p))
                .ToList();

            var columnList = string.Join(", ", props.Select(p => p.Name));
            var columnListWithType = string.Join(", ", newColumnsDef);

            sql.Add("PRAGMA foreign_keys=OFF;");
            sql.Add("BEGIN TRANSACTION;");

            // 1) Create new shadow table with UNIQUE constraints
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
        }
        else
        {
            // — Columns sync: only create/add/drop if no rebuild needed —
            var existingNames = existingColsByName.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 1. CREATE TABLE if empty
            if (existingNames.Count == 0)
            {
                var cols = props.Select(p => BuildColumnDefinition(p));
                sql.Add($"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", cols)});");
            }
            else
            {
                // 2. ADD missing
                foreach (var p in props)
                {
                    if (!existingNames.Contains(p.Name))
                    {
                        sql.Add($"ALTER TABLE {tableName} ADD COLUMN {BuildColumnDefinition(p)};");
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

        // — Indexes sync: only non-unique indexes, unique enforced via constraint in table —
        var existingIdx = (existingIndexes ?? Enumerable.Empty<SqliteMasterInfo>())
            .Where(i => i.type.Equals("index", StringComparison.OrdinalIgnoreCase)
                        && i.tbl_name.Equals(tableName, StringComparison.OrdinalIgnoreCase))
            .Select(i => i.name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var desired = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // [Index] attributes: skip Unique ones
        foreach (var p in props)
        {
            var att = p.GetCustomAttribute<IndexAttribute>();
            if (att != null && !att.Unique)
            {
                var idxName = att.Name ?? $"idx_{tableName}_{p.Name}";
                var ddl = $"CREATE INDEX IF NOT EXISTS {idxName} ON {tableName}({p.Name});";
                desired[idxName] = ddl;
            }
        }

        // [Join] attributes remain unchanged
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

    private static string BuildColumnDefinition(PropertyInfo p)
    {
        // Check if the property is an integer with the [Key] attribute.
        var keyAttr = p.GetCustomAttribute<KeyAttribute>();
        var isIntegerType = p.PropertyType == typeof(int) || p.PropertyType == typeof(long);

        if (keyAttr != null && isIntegerType)
        {
            // For SQLite, AUTOINCREMENT requires the type to be exactly 'INTEGER'.
            // A primary key is implicitly NOT NULL and UNIQUE.
            return $"{p.Name} INTEGER PRIMARY KEY AUTOINCREMENT";
        }

        // Original logic for all other columns.
        var typeSql = SqliteToNativeTypeMap.ToSqlType(p);
        var nullDef = GetNullabilityDefinition(p);
        var uniqueDef = GetUniqueDefinition(p);
    
        return $"{p.Name} {typeSql}{nullDef}{uniqueDef}";
    }

    private static string GetNullabilityDefinition(PropertyInfo p)
    {
        if (p.GetCustomAttribute<NotNullAttribute>() != null)
            return " NOT NULL";
        if (p.GetCustomAttribute<AllowNullAttribute>() != null)
            return string.Empty;
        if (p.PropertyType.IsValueType)
            return p.PropertyType.IsNullableType() ? string.Empty : " NOT NULL";
        return string.Empty;
    }

    private static string GetUniqueDefinition(PropertyInfo p)
    {
        var att = p.GetCustomAttribute<IndexAttribute>();
        return att != null && att.Unique ? " UNIQUE" : string.Empty;
    }
}
