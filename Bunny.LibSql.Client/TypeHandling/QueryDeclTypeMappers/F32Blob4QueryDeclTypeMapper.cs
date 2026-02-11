using System.Reflection;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.Types;

namespace Bunny.LibSql.Client.TypeHandling.QueryDeclTypeMappers;

public static class F32Blob4QueryDeclTypeMapper
{
    public static void MapF32BlobToLocalValue(QueryDeclType columnDeclaredType, PropertyInfo pi, object obj, LibSqlValue libSqlValue)
    {
        if (pi.PropertyType != typeof(F32Blob))
        {
            return;
        }

        var expectedSize = F32Blob.GetSize(pi);

        if (libSqlValue.Base64 != null)
        {
            if (!IsExpectedSize(expectedSize, libSqlValue.Base64.Length))
            {
                return;
            }
            var f32Blob = new F32Blob(libSqlValue.Base64);
            pi.SetValue(obj, f32Blob);
            return;
        }
        else if (libSqlValue.Value == null)
        {
            pi.SetValue(obj, null);
            return;
        }
        else if (libSqlValue.Value is JsonElement element && element.ValueKind == JsonValueKind.Null)
        {
            pi.SetValue(obj, null);
            return;
        }
        else if (TryReadBase64Bytes(libSqlValue.Value, out var bytes))
        {
            if (!IsExpectedSize(expectedSize, bytes.Length))
            {
                return;
            }
            var f32Blob = new F32Blob(bytes);
            pi.SetValue(obj, f32Blob);
            return;
        }
    }

    private static bool TryReadBase64Bytes(object value, out byte[] bytes)
    {
        bytes = [];

        if (value is string a)
        {
            return TryDecodeBase64(a, out bytes);
        }
        else if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                var base64 = element.GetString();
                if (base64 == null)
                {
                    return false;
                }

                return TryDecodeBase64(base64, out bytes);
            }

            return false;
        }

        return false;
    }

    private static bool IsExpectedSize(int expectedSize, int actualSize)
    {
        return expectedSize == actualSize;
    }

    private static bool TryDecodeBase64(string base64, out byte[] bytes)
    {
        bytes = [];

        try
        {
            bytes = Convert.FromBase64String(base64);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
