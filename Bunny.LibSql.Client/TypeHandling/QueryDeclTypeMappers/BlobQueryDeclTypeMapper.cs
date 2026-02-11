using System.Reflection;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.TypeHandling.QueryDeclTypeMappers;

public static class BlobQueryDeclTypeMapper
{
    public static void MapBlobToLocalValue(QueryDeclType columnDeclaredType, PropertyInfo pi, object obj, LibSqlValue libSqlValue)
    {
        if (pi.PropertyType == typeof(string))
        {
            if (libSqlValue.Value == null && libSqlValue.Base64 == null)
            {
                pi.SetValue(obj, null);
                return;
            }
            else if (libSqlValue.Value is string val)
            {
                pi.SetValue(obj, val);
            }
            else if (libSqlValue.Value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Null)
                {
                    pi.SetValue(obj, null);
                    return;
                }
                else if (element.ValueKind == JsonValueKind.String)
                {
                    pi.SetValue(obj, element.GetString());
                }
            }
            else if (libSqlValue.Base64 != null)
            {
                pi.SetValue(obj, Convert.ToBase64String(libSqlValue.Base64));
            }
        }
        else if (pi.PropertyType == typeof(byte[]))
        {
            if (libSqlValue.Value == null && libSqlValue.Base64 == null)
            {
                pi.SetValue(obj, null);
                return;
            }
            else if (libSqlValue.Base64 != null)
            {
                pi.SetValue(obj, libSqlValue.Base64);
            }
            else if (libSqlValue.Value is string val)
            {
                var bytes = Convert.FromBase64String(val);
                pi.SetValue(obj, bytes);
            }
            else if (libSqlValue.Value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Null)
                {
                    pi.SetValue(obj, null);
                    return;
                }
                else if (element.ValueKind == JsonValueKind.String)
                {
                    var text = element.GetString();
                    if (text != null)
                    {
                        var bytes = Convert.FromBase64String(text);
                        pi.SetValue(obj, bytes);
                    }
                    else
                    {
                        pi.SetValue(obj, null);
                    }
                }
            }
        }
    }
}
