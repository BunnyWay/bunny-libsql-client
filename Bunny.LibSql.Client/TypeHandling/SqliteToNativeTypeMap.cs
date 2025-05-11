namespace Bunny.LibSql.Client.SQL;

public static class SqliteToNativeTypeMap
{
    public static string ToSqlType(Type t)
    {
        if (t == typeof(int)  || t == typeof(long) || t == typeof(bool) || t == typeof(DateTime))
            return "INTEGER";
        if (t == typeof(float)|| t == typeof(double) || t == typeof(decimal))
            return "REAL";
        if (t == typeof(string))
            return "TEXT";
        if (t == typeof(byte[]))
            return "BLOB";
        
        // Nullable types
        if (t == typeof(int?)  || t == typeof(long?) || t == typeof(bool?) || t == typeof(DateTime?))
            return "INTEGER";
        if (t == typeof(float?)|| t == typeof(double?) || t == typeof(decimal?))
            return "REAL";
        
        throw new NotSupportedException($"No mapping for {t.Name}");
    }
}