using Bunny.LibSql.Client.LINQ;

namespace Bunny.LibSql.Client;

public partial class LibSqlTable<T>
{
    public async Task InsertAsync(T item)
    {
        if(item == null)
            throw new ArgumentNullException(nameof(item));
        
        var query = QueryBuilder.BuildInsertQuery<T>(TableName, item);
        var resp = await Db.Client.QueryAsync(query);
        AssignLastInsertRowId(item, resp);
    }

    public async Task DeleteAsync(T item)
    {
        var keyValue =  PrimaryKeyProperty.GetValue(item);
        if (keyValue == null)
        {
            throw new ArgumentException($"The item does not have a value for the primary key '{PrimaryKeyProperty}'.");
        }
        
        var query = QueryBuilder.BuildDeleteQuery(TableName, PrimaryKeyProperty.Name, keyValue);
        await Db.Client.QueryAsync(query);
    }
    
    // TODO: update
    
    private void AssignLastInsertRowId(T item, PipelineResponse? pipelineResponse)
    {
        var newKey = pipelineResponse?.Results?.FirstOrDefault()?.Response?.Result?.LastInsertRowId;
        if (newKey == null)
        {
            throw new InvalidOperationException("Failed to retrieve the last insert row ID.");
        }
        
        // TODO: bool, short etc
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
}