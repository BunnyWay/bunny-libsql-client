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
    
    // TODO: update this and add a test
    private void AssignLastInsertRowId(T item, PipelineResponse? pipelineResponse, int itemIndex = 0)
    {
        // Item index uses a different last_insert_rowid for each item
        var newKey = pipelineResponse?.Results?.Skip(itemIndex).FirstOrDefault()?.Response?.Result?.LastInsertRowId;
        if (newKey == null)
        {
            throw new InvalidOperationException("Failed to retrieve the last insert row ID.");
        }
        
        // TODO: bool, short etc (document / verify the supported properties)
        var keyProperty = GetPrimaryKeyProperty();
        if (keyProperty.PropertyType == typeof(int))
        {
            keyProperty.SetValue(item, newKey as int?);
        }
        else if (keyProperty.PropertyType == typeof(float))
        {
            keyProperty.SetValue(item, newKey as float?);
        }
        else if (keyProperty.PropertyType == typeof(long))
        {
            keyProperty.SetValue(item, newKey as long?);
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