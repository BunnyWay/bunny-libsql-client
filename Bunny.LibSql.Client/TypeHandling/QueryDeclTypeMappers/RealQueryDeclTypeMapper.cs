using System.Globalization;
using System.Reflection;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.TypeHandling.QueryDeclTypeMappers;

public static class RealQueryDeclTypeMapper
{
    public static void MapRealToLocalValue(QueryDeclType columnDeclaredType, PropertyInfo pi, object obj, LibSqlValue libSqlValue)
    {
        if (pi.PropertyType == typeof(double))
        {
            if (libSqlValue.Value is double val)
            {
                pi.SetValue(obj, val);
            }
        }
        else if (pi.PropertyType == typeof(float))
        {
            if (libSqlValue.Value is double val)
            {
                pi.SetValue(obj, Convert.ToSingle(val));
            }
        }
    }
}