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
        if (pi.PropertyType != typeof(string))
        {
            return;
        }
        else if (libSqlValue.Value == null)
        {
            pi.SetValue(obj, null);
        }
        else
        {
            if (libSqlValue.Value is string s)
            {
                pi.SetValue(obj, s);
            }
            else if (libSqlValue.Value is JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.Null:
                        pi.SetValue(obj, null);
                        break;
                    case JsonValueKind.String:
                        pi.SetValue(obj, element.GetString());
                        break;
                    case JsonValueKind.Number:
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                    case JsonValueKind.Array:
                    case JsonValueKind.Object:
                        pi.SetValue(obj, element.GetRawText());
                        break;
                    default:
                        pi.SetValue(obj, element.ToString());
                        break;
                }
            }
            else
            {
                if (libSqlValue.Value is IFormattable formattable)
                {
                    pi.SetValue(obj, formattable.ToString(null, CultureInfo.InvariantCulture));
                }
                else
                {
                    pi.SetValue(obj, libSqlValue.Value.ToString());
                }
            }
        }
    }
}
