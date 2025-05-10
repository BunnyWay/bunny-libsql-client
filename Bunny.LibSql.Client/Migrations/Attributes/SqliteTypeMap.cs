namespace Bunny.LibSql.Client.ORM.Attributes;

public static class SqliteTypeMap
{
    public static string ToSqlType(Type t)
    {
        if (t == typeof(int)  || t == typeof(long) || t == typeof(bool))
            return "INTEGER";
        if (t == typeof(float)|| t == typeof(double) || t == typeof(decimal))
            return "REAL";
        if (t == typeof(string)|| t == typeof(DateTime))
            return "TEXT";
        if (t == typeof(byte[]))
            return "BLOB";
        throw new NotSupportedException($"No mapping for {t.Name}");
    }
}