using System.Globalization;
using System.Reflection;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.TypeHandling.QueryDeclTypeMappers;

public static class BlobQueryDeclTypeMapper
{
    public static void MapBlobToLocalValue(QueryDeclType columnDeclaredType, PropertyInfo pi, object obj, LibSqlValue libSqlValue)
    {
        if (pi.PropertyType == typeof(string))
        {
            if (libSqlValue.Value is string val)
            {
                pi.SetValue(obj, val);
            }
        }
        else if (pi.PropertyType == typeof(byte[]))
        {
            if (libSqlValue.Value is string val)
            {
                var bytes = Convert.FromBase64String(val);
                pi.SetValue(obj, bytes);
            }
        }
    }
}