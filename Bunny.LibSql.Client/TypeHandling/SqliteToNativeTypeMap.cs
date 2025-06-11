using System.Reflection;
using Bunny.LibSql.Client.Types;

namespace Bunny.LibSql.Client.SQL;

public static class SqliteToNativeTypeMap
{
    public static string ToSqlType(PropertyInfo property)
    {
        var t = property.PropertyType;
        if (t == typeof(int)  || t == typeof(long) || t == typeof(bool) || t == typeof(DateTime))
            return "INTEGER";
        if (t == typeof(float)|| t == typeof(double) || t == typeof(decimal))
            return "REAL";
        if (t == typeof(string))
            return "TEXT";
        if (t == typeof(byte[]))
            return "BLOB";
        if (t == typeof(F32Blob))
        {
            var size = F32Blob.GetSize(property);
            return $"F32_BLOB({size})";
        }
        
        // Nullable types
        if (t == typeof(int?)  || t == typeof(long?) || t == typeof(bool?) || t == typeof(DateTime?))
            return "INTEGER";
        if (t == typeof(float?)|| t == typeof(double?) || t == typeof(decimal?))
            return "REAL";
        
        throw new NotSupportedException($"No mapping for {t.Name}");
    }
}