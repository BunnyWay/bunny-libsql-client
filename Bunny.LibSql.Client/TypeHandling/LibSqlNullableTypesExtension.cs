namespace Bunny.LibSql.Client.TypeHandling;

public static class LibSqlNullableTypesExtension
{
    public static bool IsNullableType(this Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return true;

        return type.IsClass && !type.IsValueType;
    }
}