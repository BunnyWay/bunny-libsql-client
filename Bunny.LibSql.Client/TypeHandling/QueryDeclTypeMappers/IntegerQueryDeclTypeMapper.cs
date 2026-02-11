using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.TypeHandling.QueryDeclTypeMappers;

public static class IntegerQueryDeclTypeMapper
{
    public static void MapIntegerToLocalValue(QueryDeclType columnDeclaredType, PropertyInfo pi, object obj, LibSqlValue libSqlValue)
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

        if (!IsSupportedTargetType(targetType))
        {
            return;
        }

        if (!TryReadIntegerAsLong(libSqlValue, out var val, out _))
        {
            return;
        }
        else if (targetType == typeof(long))
        {
            pi.SetValue(obj, underlyingType != null ? (long?)val : val);
        }
        else if (targetType == typeof(int))
        {
            if (!TryConvertToInt32(val, out var intVal))
            {
                return;
            }
            pi.SetValue(obj, underlyingType != null ? (int?)intVal : intVal);
        }
        else if (targetType == typeof(short))
        {
            if (!TryConvertToInt16(val, out var shortVal))
            {
                return;
            }
            pi.SetValue(obj, underlyingType != null ? (short?)shortVal : shortVal);
        }
        else if (targetType == typeof(byte))
        {
            if (!TryConvertToByte(val, out var byteVal))
            {
                return;
            }
            pi.SetValue(obj, underlyingType != null ? (byte?)byteVal : byteVal);
        }
        else if (targetType == typeof(sbyte))
        {
            if (!TryConvertToSByte(val, out var sbyteVal))
            {
                return;
            }
            pi.SetValue(obj, underlyingType != null ? (sbyte?)sbyteVal : sbyteVal);
        }
        else if (targetType == typeof(ushort))
        {
            if (!TryConvertToUInt16(val, out var ushortVal))
            {
                return;
            }
            pi.SetValue(obj, underlyingType != null ? (ushort?)ushortVal : ushortVal);
        }
        else if (targetType == typeof(uint))
        {
            if (!TryConvertToUInt32(val, out var uintVal))
            {
                return;
            }
            pi.SetValue(obj, underlyingType != null ? (uint?)uintVal : uintVal);
        }
        else if (targetType == typeof(ulong))
        {
            if (!TryConvertToUInt64(val, out var ulongVal))
            {
                return;
            }
            pi.SetValue(obj, underlyingType != null ? (ulong?)ulongVal : ulongVal);
        }
        else if (targetType == typeof(bool))
        {
            var boolVal = val != 0;
            pi.SetValue(obj, underlyingType != null ? (bool?)boolVal : boolVal);
        }
        else if (targetType == typeof(DateTime))
        {
            var dateVal = val.ToUnixDateTime();
            pi.SetValue(obj, underlyingType != null ? (DateTime?)dateVal : dateVal);
        }
        else if (targetType == typeof(DateTimeOffset))
        {
            var dateVal = DateTimeOffset.FromUnixTimeSeconds(val);
            pi.SetValue(obj, underlyingType != null ? (DateTimeOffset?)dateVal : dateVal);
        }
    }

    private static bool TryReadIntegerAsLong(LibSqlValue libSqlValue, out long value, out string? error)
    {
        value = 0;
        error = null;

        if (libSqlValue.Value == null)
        {
            error = "null";
            return false;
        }

        switch (libSqlValue.Value)
        {
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
            case sbyte sb:
                value = sb;
                return true;
            case ushort us:
                value = us;
                return true;
            case uint ui:
                value = ui;
                return true;
            case ulong ul:
                if (ul > long.MaxValue)
                {
                    error = "unsigned value out of range";
                    return false;
                }
                value = (long)ul;
                return true;
            case JsonElement element:
                return TryReadIntegerFromJsonElement(element, out value, out error);
            case string s:
                if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    value = parsed;
                    return true;
                }

                error = "invalid integer string";
                return false;
            default:
                if (libSqlValue.Value is IFormattable formattable)
                {
                    var text = formattable.ToString(null, CultureInfo.InvariantCulture);
                    if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedFormattable))
                    {
                        value = parsedFormattable;
                        return true;
                    }

                    error = "invalid integer value";
                    return false;
                }

                error = libSqlValue.Value.GetType().Name;
                return false;
        }
    }

    private static bool TryReadIntegerFromJsonElement(JsonElement element, out long value, out string? error)
    {
        value = 0;
        error = null;

        if (element.ValueKind == JsonValueKind.Number)
        {
            if (element.TryGetInt64(out var l))
            {
                value = l;
                return true;
            }
            else if (element.TryGetUInt64(out var ul) && ul <= long.MaxValue)
            {
                value = (long)ul;
                return true;
            }

            error = "JSON number is not a valid 64-bit integer";
            return false;
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            var text = element.GetString();
            if (text == null)
            {
                error = "null JSON string";
                return false;
            }
            else if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }

            error = "invalid integer string";
            return false;
        }
        else if (element.ValueKind == JsonValueKind.Null)
        {
            error = "null";
            return false;
        }

        error = $"JSON {element.ValueKind}";
        return false;
    }

    private static bool IsSupportedTargetType(Type targetType)
    {
        return targetType == typeof(long)
               || targetType == typeof(int)
               || targetType == typeof(short)
               || targetType == typeof(byte)
               || targetType == typeof(sbyte)
               || targetType == typeof(ushort)
               || targetType == typeof(uint)
               || targetType == typeof(ulong)
               || targetType == typeof(bool)
               || targetType == typeof(DateTime)
               || targetType == typeof(DateTimeOffset);
    }

    private static bool TryConvertToInt32(long value, out int result)
    {
        result = 0;
        if (value < int.MinValue || value > int.MaxValue)
        {
            return false;
        }

        result = (int)value;
        return true;
    }

    private static bool TryConvertToInt16(long value, out short result)
    {
        result = 0;
        if (value < short.MinValue || value > short.MaxValue)
        {
            return false;
        }

        result = (short)value;
        return true;
    }

    private static bool TryConvertToByte(long value, out byte result)
    {
        result = 0;
        if (value < byte.MinValue || value > byte.MaxValue)
        {
            return false;
        }

        result = (byte)value;
        return true;
    }

    private static bool TryConvertToSByte(long value, out sbyte result)
    {
        result = 0;
        if (value < sbyte.MinValue || value > sbyte.MaxValue)
        {
            return false;
        }

        result = (sbyte)value;
        return true;
    }

    private static bool TryConvertToUInt16(long value, out ushort result)
    {
        result = 0;
        if (value < ushort.MinValue || value > ushort.MaxValue)
        {
            return false;
        }

        result = (ushort)value;
        return true;
    }

    private static bool TryConvertToUInt32(long value, out uint result)
    {
        result = 0;
        if (value < uint.MinValue || value > uint.MaxValue)
        {
            return false;
        }

        result = (uint)value;
        return true;
    }

    private static bool TryConvertToUInt64(long value, out ulong result)
    {
        result = 0;
        if (value < 0)
        {
            return false;
        }

        result = (ulong)value;
        return true;
    }
}
