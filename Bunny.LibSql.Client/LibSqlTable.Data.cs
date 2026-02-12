using System.ComponentModel.DataAnnotations;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.LINQ;
using Bunny.LibSql.Client.SQL;

namespace Bunny.LibSql.Client;

// TODO: add helper methods for multi-update, multi-delete and multi-insert
public partial class LibSqlTable<T>
{
    public async Task InsertAsync(T item)
    {
        if(item == null)
            throw new ArgumentNullException(nameof(item));

        if (AutoValidateEntities)
        {
            ValidateEntity(item);
        }

        var query = SqlQueryBuilder.BuildInsertQuery<T>(TableName, item);
        var resp = await Db.Client.QueryAsync(query);
        AssignLastInsertRowId(item, resp);
    }

    public async Task UpdateAsync(T item)
    {
        var keyValue =  PrimaryKeyProperty.GetValue(item);
        if (keyValue == null)
        {
            throw new ArgumentException($"The item does not have a value for the primary key '{PrimaryKeyProperty}'.");
        }

        var query = SqlQueryBuilder.BuildUpdateQuery(TableName, item, PrimaryKeyProperty.Name, keyValue);
        await Db.Client.QueryAsync(query);
    }

    public async Task DeleteAsync(T item)
    {
        var keyValue =  PrimaryKeyProperty.GetValue(item);
        if (keyValue == null)
        {
            throw new ArgumentException($"The item does not have a value for the primary key '{PrimaryKeyProperty}'.");
        }

        var query = SqlQueryBuilder.BuildDeleteQuery(TableName, PrimaryKeyProperty.Name, keyValue);
        await Db.Client.QueryAsync(query);
    }

    private void AssignLastInsertRowId(T item, PipelineResponse? pipelineResponse, int itemIndex = 0)
    {
        var keyProperty = GetPrimaryKeyProperty();
        var currentValue = keyProperty.GetValue(item);

        // If the key is a string and already has a value (e.g., a UUID), do not overwrite it.
        if (currentValue is string strVal && !string.IsNullOrEmpty(strVal))
        {
            return;
        }

        // For numeric types, overwrite only if the value is 0 (default)
        if (currentValue is int intVal && intVal != 0 ||
            currentValue is long longVal && longVal != 0)
        {
            return;
        }

        var rawKey = pipelineResponse?.Results?.Skip(itemIndex).FirstOrDefault()?.Response?.Result?.LastInsertRowId;
        if (rawKey == null) return;

        // LastInsertRowId may be deserialized as JsonElement from JSON responses
        var newKey = rawKey is System.Text.Json.JsonElement jsonEl ? jsonEl.GetInt64() : Convert.ToInt64(rawKey);

        if (keyProperty.PropertyType == typeof(int))
        {
            keyProperty.SetValue(item, (int)newKey);
        }
        else if (keyProperty.PropertyType == typeof(long))
        {
            keyProperty.SetValue(item, newKey);
        }
        else if (keyProperty.PropertyType == typeof(string))
        {
            keyProperty.SetValue(item, newKey.ToString());
        }
        else
        {
            throw new InvalidOperationException($"Unsupported primary key type: {keyProperty.PropertyType.Name}");
        }
    }

    private void ValidateEntity(T entity)
    {
        var ctx = new ValidationContext(entity);
        Validator.ValidateObject(entity, ctx, validateAllProperties: true);
    }
}