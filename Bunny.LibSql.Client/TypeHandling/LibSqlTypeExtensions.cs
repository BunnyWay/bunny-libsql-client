public static class LibSqlTypeExtensions
{
    public static bool IsLibSqlSupportedType(this Type type)
    {
        if (type == typeof(int))
            return true;
        if (type == typeof(long))
            return true;
        if (type == typeof(float))
            return true;
        if (type == typeof(string))
            return true;
        if (type == typeof(double))
            return true;
        if (type == typeof(bool))
            return true;
        if (type == typeof(DateTime))
            return true;
        if (type == typeof(byte[]))
            return true;
        
        if (type == typeof(int?))
            return true;
        if (type == typeof(long?))
            return true;
        if (type == typeof(float?))
            return true;
        if (type == typeof(double?))
            return true;
        if (type == typeof(bool?))
            return true;
        if (type == typeof(DateTime?))
            return true;

        return false;
    }
}