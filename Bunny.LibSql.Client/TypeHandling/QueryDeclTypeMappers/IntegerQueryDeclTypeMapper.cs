using System.Globalization;
using System.Reflection;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.TypeHandling.QueryDeclTypeMappers;

public static class IntegerQueryDeclTypeMapper
{
    public static void MapIntegerToLocalValue(QueryDeclType columnDeclaredType, PropertyInfo pi, object obj, LibSqlValue libSqlValue)
    {
        if (pi.PropertyType == typeof(long))
        {
            var val = ReadIntegerAsLong(libSqlValue);
            if (val != null)
            {
                pi.SetValue(obj, val.Value);
            }
        }
        else if (pi.PropertyType == typeof(int))
        {
            var val = ReadIntegerAsLong(libSqlValue);
            if (val != null)
            {
                pi.SetValue(obj, Convert.ToInt32(val.Value));
            }
        }
        else if (pi.PropertyType == typeof(bool))
        {
            var val = ReadIntegerAsLong(libSqlValue);
            if (val != null)
            {
                pi.SetValue(obj, val.Value != 0);
            }
        }
        else if (pi.PropertyType == typeof(DateTime))
        {
            var val = ReadIntegerAsLong(libSqlValue);
            if (val != null)
            {
                pi.SetValue(obj, val.Value.ToUnixDateTime());
            }
        }
    }
    
    private static long? ReadIntegerAsLong(LibSqlValue libSqlValue)
    {
        if (!long.TryParse(libSqlValue.Value?.ToString(), CultureInfo.InvariantCulture, out var valueParsed))
        {
            return null;
        }

        return valueParsed;
    }
}