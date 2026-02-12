using System.ComponentModel.DataAnnotations;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.LINQ;
using Bunny.LibSql.Client.SQL;

namespace Bunny.LibSql.Client;

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

    public async Task InsertManyAsync(IEnumerable<T> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0) return;

        if (AutoValidateEntities)
        {
            foreach (var item in itemList)
                ValidateEntity(item);
        }

        var queries = itemList.Select(item => SqlQueryBuilder.BuildInsertQuery<T>(TableName, item)).ToList();
        var resp = await Db.Client.QueryMultipleAsync(queries);

        for (var i = 0; i < itemList.Count; i++)
            AssignLastInsertRowId(itemList[i], resp, i);
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

    public async Task UpdateManyAsync(IEnumerable<T> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0) return;

        var queries = itemList.Select(item =>
        {
            var keyValue = PrimaryKeyProperty.GetValue(item)
                ?? throw new ArgumentException($"The item does not have a value for the primary key '{PrimaryKeyProperty}'.");
            return SqlQueryBuilder.BuildUpdateQuery(TableName, item, PrimaryKeyProperty.Name, keyValue);
        }).ToList();

        await Db.Client.QueryMultipleAsync(queries);
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

    public async Task DeleteManyAsync(IEnumerable<T> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0) return;

        var queries = itemList.Select(item =>
        {
            var keyValue = PrimaryKeyProperty.GetValue(item)
                ?? throw new ArgumentException($"The item does not have a value for the primary key '{PrimaryKeyProperty}'.");
            return SqlQueryBuilder.BuildDeleteQuery(TableName, PrimaryKeyProperty.Name, keyValue);
        }).ToList();

        await Db.Client.QueryMultipleAsync(queries);
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