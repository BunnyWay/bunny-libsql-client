using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.TypeHandling.QueryDeclTypeMappers;

public static class RealQueryDeclTypeMapper
{
    public static void MapRealToLocalValue(QueryDeclType columnDeclaredType, PropertyInfo pi, object obj, LibSqlValue libSqlValue)
    {
        var underlyingType = Nullable.GetUnderlyingType(pi.PropertyType);
        var targetType = underlyingType ?? pi.PropertyType;

        if (libSqlValue.Value == null)
        {
            if (underlyingType != null)
            {
                pi.SetValue(obj, null);
            }

            return;
        }
        else if (!TryReadDouble(libSqlValue, out var val, out _))
        {
            return;
        }
        else if (targetType == typeof(double))
        {
            pi.SetValue(obj, underlyingType != null ? (double?)val : val);
        }
        else if (targetType == typeof(float))
        {
            if (!TryConvertToSingle(val, out var floatVal))
            {
                return;
            }

            pi.SetValue(obj, underlyingType != null ? (float?)floatVal : floatVal);
        }
    }

    private static bool TryReadDouble(LibSqlValue libSqlValue, out double value, out string? error)
    {
        value = 0d;
        error = null;

        if (libSqlValue.Value == null)
        {
            error = "null";
            return false;
        }

        switch (libSqlValue.Value)
        {
            case double d:
                value = d;
                return true;
            case float f:
                value = f;
                return true;
            case decimal m:
                value = (double)m;
                return true;
            case long l:
                value = l;
                return true;
            case int i:
                value = i;
                return true;
            case short s:
                value = s;
                return true;
            case byte b:
                value = b;
                return true;
            case ulong ul:
                value = ul;
                return true;
            case uint ui:
                value = ui;
                return true;
            case ushort us:
                value = us;
                return true;
            case sbyte sb:
                value = sb;
                return true;
            case JsonElement element:
                return TryReadDoubleFromJsonElement(element, out value, out error);
            case string s:
                if (double.TryParse(
                    s,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out value))
                {
                    return true;
                }

                error = "invalid numeric string";
                return false;
            default:
                if (libSqlValue.Value is IFormattable formattable)
                {
                    if (double.TryParse(
                        formattable.ToString(null, CultureInfo.InvariantCulture),
                        NumberStyles.Float | NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture,
                        out value))
                    {
                        return true;
                    }

                    error = "invalid numeric value";
                    return false;
                }

                error = libSqlValue.Value.GetType().Name;
                return false;
        }
    }

    private static bool TryReadDoubleFromJsonElement(JsonElement element, out double value, out string? error)
    {
        value = 0d;
        error = null;

        if (element.ValueKind == JsonValueKind.Number)
        {
            if (element.TryGetDouble(out var d))
            {
                value = d;
                return true;
            }
            else if (element.TryGetInt64(out var l))
            {
                value = l;
                return true;
            }
            else if (element.TryGetDecimal(out var m))
            {
                value = (double)m;
                return true;
            }

            error = "JSON number is not a valid double";
            return false;
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            var s = element.GetString();
            if (s == null)
            {
                error = "null JSON string";
                return false;
            }
            else if (double.TryParse(
                s,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out value))
            {
                return true;
            }

            error = "invalid numeric string";
            return false;
        }

        error = $"JSON {element.ValueKind}";
        return false;
    }

    private static bool TryConvertToSingle(double value, out float floatValue)
    {
        floatValue = 0f;

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return false;
        }

        if (value > float.MaxValue || value < float.MinValue)
        {
            return false;
        }

        var floatVal = (float)value;
        if (floatVal == 0f && value != 0d)
        {
            return false;
        }

        floatValue = floatVal;
        return true;
    }
}
