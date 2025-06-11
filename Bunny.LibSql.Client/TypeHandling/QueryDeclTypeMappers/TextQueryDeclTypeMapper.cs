using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.TypeHandling.QueryDeclTypeMappers;

public static class TextQueryDeclTypeMapper
{
    public static void MapTextToLocalValue(QueryDeclType columnDeclaredType, PropertyInfo pi, object obj, LibSqlValue libSqlValue)
    {
        if (libSqlValue.Value == null)
        {
            pi.SetValue(obj, null);
        }
        else
        {
            pi.SetValue(obj, ((JsonElement)libSqlValue.Value).GetString());
        }
    }
}