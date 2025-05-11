namespace Bunny.LibSql.Client.SQL;

public class SqlQueryBuilder
{
    public static SqlQuery BuildInsertQuery<T>(string tableName, object obj)
    {
        var type = typeof(T);
        var properties = type.GetProperties();
        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new List<object>();

        foreach (var property in properties)
        {
            if (property.PropertyType.IsLibSqlSupportedType() && property.GetValue(obj) != null)
            {
                columns.Add(property.Name);
                values.Add($"?");
                parameters.Add(property.GetValue(obj));
            }
        }

        var columnsString = string.Join(", ", columns);
        var valuesString = string.Join(", ", values);

        var query = $"INSERT INTO {tableName} ({columnsString}) VALUES ({valuesString})";

        return new SqlQuery(query, parameters.ToArray());
    }

    // TODO: check if this works for floats
    public static SqlQuery BuildDeleteQuery(string tableName, string primaryKey, object primaryKeyValue)
    {
        return new SqlQuery($"DELETE FROM {tableName} WHERE {primaryKey} = ?", [primaryKeyValue]);
    }
}