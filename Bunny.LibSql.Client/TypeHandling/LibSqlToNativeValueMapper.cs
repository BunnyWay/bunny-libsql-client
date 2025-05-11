using System.Globalization;
using System.Reflection;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.TypeHandling.QueryDeclTypeMappers;

namespace Bunny.LibSql.Client.TypeHandling;

// This is here for better code navigation (TypeHandling folder)

public static class LibSqlToNativeValueMapper
{
    public static void AssignLibSqlValueToNativeProperty(QueryDeclType? columnDeclaredType, PropertyInfo pi, object obj, LibSqlValue libSqlValue)
    {
        // If we don't get a defined column type (eg: for some system queries), we imply it from the value type itself
        if (columnDeclaredType == null)
        {
            columnDeclaredType = GetQueryDeclTypeFromValue(libSqlValue);
        }
        
        if (columnDeclaredType != null)
        {
            switch (columnDeclaredType)
            {
                case QueryDeclType.Integer: case QueryDeclType.Bigint: case QueryDeclType.Boolean: case QueryDeclType.Int:
                    IntegerQueryDeclTypeMapper.MapIntegerToLocalValue(columnDeclaredType.Value, pi, obj, libSqlValue);
                    break;
                case QueryDeclType.Real:
                    RealQueryDeclTypeMapper.MapRealToLocalValue(columnDeclaredType.Value, pi, obj, libSqlValue);
                    break;
                case QueryDeclType.Text:
                    TextQueryDeclTypeMapper.MapTextToLocalValue(columnDeclaredType.Value, pi, obj, libSqlValue);
                    break;
                case QueryDeclType.Blob:
                    BlobQueryDeclTypeMapper.MapBlobToLocalValue(columnDeclaredType.Value, pi, obj, libSqlValue);
                    break;
            }
        }
    }

    private static QueryDeclType? GetQueryDeclTypeFromValue(LibSqlValue libSqlValue)
    {
        if (libSqlValue.Type == LibSqlValueType.Float)
        {
            return QueryDeclType.Real;
        }
        else if (libSqlValue.Type == LibSqlValueType.Integer)
        {
            return QueryDeclType.Integer;
        }
        else if (libSqlValue.Type == LibSqlValueType.Text)
        {
            return QueryDeclType.Text;
        }
        else if (libSqlValue.Type == LibSqlValueType.Null)
        {
            return null;
        }

        return null;
    }
}