using System.ComponentModel.DataAnnotations;
using Bunny.LibSql.Client.Attributes;
using Bunny.LibSql.Client.Types;

namespace Bunny.LibSql.Client.SQL;

public class SqlQueryBuilder
{
    public static SqlQuery BuildUpdateQuery<T>(string tableName, T item, string primaryKey, object primaryKeyValue)
    {
        var type = typeof(T);
        var properties = type.GetProperties();
        var setClauses = new List<string>();
        var parameters = new List<object>();
        foreach (var property in properties)
        {
            if (property.PropertyType.IsLibSqlSupportedType() && property.GetValue(item) != null)
            {
                setClauses.Add($"{property.Name} = ?");
                parameters.Add(property.GetValue(item));
            }
        }
        var setClauseString = string.Join(", ", setClauses);
        var query = $"UPDATE {tableName} SET {setClauseString} WHERE {primaryKey} = ?";
        parameters.Add(primaryKeyValue);
        
        return new SqlQuery(query, parameters.ToArray());
    }
    
    public static SqlQuery BuildInsertQuery<T>(string tableName, T obj)
    {
        var type = typeof(T);
        var properties = type.GetProperties();
        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new List<object>();

        foreach (var property in properties)
        {
            if (property.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0 && 
                (property.PropertyType == typeof(int) || property.PropertyType == typeof(long)))
            {
                continue;
            }
            
            object? value = property.GetValue(obj);
            if (property.PropertyType.IsLibSqlSupportedType())
            {
                if(value is ILibSqlType libSqlType)
                {
                    value = libSqlType.GetLibSqlJsonValue();
                }
                
                columns.Add(property.Name);
                values.Add($"?");
                parameters.Add(value);
            }
        }

        var columnsString = string.Join(", ", columns);
        var valuesString = string.Join(", ", values);

        var query = $"INSERT INTO {tableName} ({columnsString}) VALUES ({valuesString})";

        return new SqlQuery(query, parameters.ToArray());
    }

    public static SqlQuery BuildDeleteQuery(string tableName, string primaryKey, object primaryKeyValue)
    {
        return new SqlQuery($"DELETE FROM {tableName} WHERE {primaryKey} = ?", [primaryKeyValue]);
    }
}