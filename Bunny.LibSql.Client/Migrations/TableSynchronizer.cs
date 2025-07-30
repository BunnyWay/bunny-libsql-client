using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using Bunny.LibSql.Client.Attributes;
using Bunny.LibSql.Client.Migrations.InternalModels;
using Bunny.LibSql.Client.SQL;
using Bunny.LibSql.Client.TypeHandling;

namespace Bunny.LibSql.Client.Migrations;

public static class TableSynchronizer
{
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

        var existingColsByName = (existingColumns ?? [])
            .ToDictionary(c => c.name, c => c, StringComparer.OrdinalIgnoreCase);

        // 0. Detect any type or constraint changes by comparing components
        var changedProps = props
            .Where(p =>
            {
                if (!existingColsByName.TryGetValue(p.Name, out var colInfo))
                {
                    return false; // This is a new column, not a changed one
                }

                // Compare each aspect of the column definition
                var desiredType = SqliteToNativeTypeMap.ToSqlType(p);
                var isPk = IsPrimaryKey(p);

                // For PKs, SQLite may report the type differently.
                // An `INTEGER PRIMARY KEY` is the only type that supports AUTOINCREMENT.
                if (isPk && desiredType == "INTEGER" && colInfo.type.Equals("INTEGER", StringComparison.OrdinalIgnoreCase))
                {
                    // It's an integer primary key, types match. Check other constraints.
                }
                else if (!desiredType.Equals(colInfo.type, StringComparison.OrdinalIgnoreCase))
                {
                    return true; // Type has changed
                }

                if (isPk != (colInfo.pk > 0)) return true; // PK status changed
                if (IsNotNull(p) != colInfo.notnull && !isPk) return true; // Nullability changed (PK is implicitly not null)
                
                // Note: The base `table_info` doesn't include UNIQUE constraints for non-PK columns.
                // We check that separately against the index list.
                if (IsUnique(p) != IsColumnUniqueInDb(p.Name, tableName, existingIndexes)) return true;

                return false;
            })
            .ToArray();


        if (changedProps.Any())
        {
            // We need to rebuild the table
            var newColumnsDef = props
                .Select(BuildDesiredColumnDefinition)
                .ToList();

            var columnList = string.Join(", ", props.Select(p => p.Name));
            var columnListWithType = string.Join(", ", newColumnsDef);

            sql.Add("PRAGMA foreign_keys=OFF;");
            sql.Add($"CREATE TABLE {tableName}_new ({columnListWithType});");
            sql.Add(
                $"INSERT INTO {tableName}_new ({columnList}) " +
                $"SELECT {columnList} FROM {tableName};"
            );
            sql.Add($"DROP TABLE {tableName};");
            sql.Add($"ALTER TABLE {tableName}_new RENAME TO {tableName};");
            sql.Add("PRAGMA foreign_keys=ON;");
        }
        else
        {
            // — Columns sync: only create/add/drop if no rebuild needed —
            var existingNames = existingColsByName.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (existingNames.Count == 0)
            {
                var cols = props.Select(BuildDesiredColumnDefinition);
                sql.Add($"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", cols)});");
            }
            else
            {
                foreach (var p in props)
                {
                    if (!existingNames.Contains(p.Name))
                    {
                        sql.Add($"ALTER TABLE {tableName} ADD COLUMN {BuildDesiredColumnDefinition(p)};");
                    }
                }

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
            .ToDictionary(i => i.name, StringComparer.OrdinalIgnoreCase);

        var desired = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // [Index] attributes: skip Unique ones as they are part of the table definition
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
            if (!existingIdx.ContainsKey(kv.Key))
                sql.Add(kv.Value);

        // Drop stale indexes (but don't drop UNIQUE constraint indexes)
        foreach (var idxName in existingIdx.Keys)
        {
            if (!desired.ContainsKey(idxName) && !IsIndexForUniqueConstraint(existingIdx[idxName]))
            {
                sql.Add($"DROP INDEX IF EXISTS {idxName};");
            }
        }
        
        return sql;
    }
    
    // --- Helper methods for checking property attributes ---
    
    private static bool IsPrimaryKey(PropertyInfo p)
    {
        var keyAttr = p.GetCustomAttribute<KeyAttribute>();
        var isIntegerType = p.PropertyType == typeof(int) || p.PropertyType == typeof(long);
        return keyAttr != null && isIntegerType;
    }

    private static bool IsNotNull(PropertyInfo p)
    {
        if (p.GetCustomAttribute<NotNullAttribute>() != null) return true;
        if (p.GetCustomAttribute<AllowNullAttribute>() != null) return false;
        // Value types (like int, bool) are not null unless they are Nullable<>
        return p.PropertyType.IsValueType && !p.PropertyType.IsNullableType();
    }

    private static bool IsUnique(PropertyInfo p)
    {
        var att = p.GetCustomAttribute<IndexAttribute>();
        return att != null && att.Unique;
    }

    // --- Helper methods for checking database state ---

    private static bool IsColumnUniqueInDb(string columnName, string tableName, IEnumerable<SqliteMasterInfo> indexes)
    {
        return (indexes ?? Enumerable.Empty<SqliteMasterInfo>())
            .Any(idx => IsIndexForUniqueConstraint(idx)
                        && idx.tbl_name.Equals(tableName, StringComparison.OrdinalIgnoreCase)
                        // This regex ensures we match the column exactly, e.g., `(Name)` and not `(NameSuffix)`
                        && Regex.IsMatch(idx.sql, $@"\(\s*`?{Regex.Escape(columnName)}`?\s*\)", RegexOptions.IgnoreCase));
    }
    
    private static bool IsIndexForUniqueConstraint(SqliteMasterInfo index)
    {
        // A UNIQUE constraint creates a unique index. The SQL definition will contain "CREATE UNIQUE INDEX".
        // SQLite also creates automatic indexes for PRIMARY KEYs, which we want to ignore here.
        return index.sql != null 
               && index.sql.Contains("CREATE UNIQUE INDEX", StringComparison.OrdinalIgnoreCase)
               && !index.name.StartsWith("sqlite_autoindex"); // Exclude PK indexes
    }
    
    private static string BuildDesiredColumnDefinition(PropertyInfo p)
    {
        if (IsPrimaryKey(p))
        {
            return $"{p.Name} INTEGER PRIMARY KEY AUTOINCREMENT";
        }

        var typeSql = SqliteToNativeTypeMap.ToSqlType(p);
        var nullDef = IsNotNull(p) ? " NOT NULL" : string.Empty;
        var uniqueDef = IsUnique(p) ? " UNIQUE" : string.Empty;
    
        return $"{p.Name} {typeSql}{nullDef}{uniqueDef}";
    }
}