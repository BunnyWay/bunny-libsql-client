public static class LibSqlTypeExtensions
{
    private static HashSet<Type> _supportedTypes =
    [
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(string),
        typeof(double)
    ];

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
        
        return _supportedTypes.Contains(type.GetType());
    }
}