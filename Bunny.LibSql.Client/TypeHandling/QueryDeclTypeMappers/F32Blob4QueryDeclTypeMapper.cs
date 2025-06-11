using System.Globalization;
using System.Reflection;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.Types;

namespace Bunny.LibSql.Client.TypeHandling.QueryDeclTypeMappers;

public static class F32Blob4QueryDeclTypeMapper
{
    public static void MapF32BlobToLocalValue(QueryDeclType columnDeclaredType, PropertyInfo pi, object obj, LibSqlValue libSqlValue)
    {
        if (pi.PropertyType == typeof(F32Blob))
        {
            if (libSqlValue.Base64 != null)
            {
                var f32Blob = new F32Blob(libSqlValue.Base64);
                pi.SetValue(obj, f32Blob);
            }
            else
            {
                throw new InvalidOperationException($"Expected a F32_BLOB of size {F32Blob.GetSize(pi)} but got {libSqlValue.Base64?.Length ?? 0}");
            }
        }
    }
}