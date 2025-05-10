using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Bunny.LibSql.Client.LINQ;
using Bunny.LibSql.Client.Extensions;
using Bunny.LibSql.Client.ORM.Attributes;

namespace Bunny.LibSql.Client;

public class LibSqlTable<T> : IQueryable<T>, ITable
{
    public readonly string TableName;
    public readonly PropertyInfo PrimaryKey;
    public Type ElementType => typeof(T);
    public Expression Expression { get; }
    public IQueryProvider Provider { get; }
    public LibSqlDatabase Db { get; set; }
    public List<JoinNavigation> JoinNavigations { get; set; } = new();
    // This is used by reflection
    public LibSqlTable(LibSqlDatabase db)
    {
        Db = db;
        TableName = GetTableName();
        PrimaryKey = GetPrimaryKeyProperty();
        Provider = new LibSqlQueryProvider<T>(this);
        Expression = Expression.Constant(this);
    }
    public LibSqlTable(IQueryProvider provider, Expression expression, LibSqlDatabase db)
    {
        Db = db;
        Provider = provider;
        Expression = expression;
        Provider = new LibSqlQueryProvider<T>(this);
        TableName = GetTableName();
    }
    public LibSqlTable(IQueryProvider provider, Expression expression, LibSqlDatabase db, List<JoinNavigation> joinNavigations)
    {
        Db = db;
        Provider = provider;
        Expression = expression;
        TableName = GetTableName();
        JoinNavigations = joinNavigations;
        Provider = new LibSqlQueryProvider<T>(this);
    }

    public LibSqlTable<T> Include<TOther>(
        Expression<Func<TOther, object>> navigationPropertyPath
    )
    {
        var leftProperty = navigationPropertyPath.GetSelectedExpressionProperty();
        var joinNavigation = GenerateJoinNavigation(leftProperty);
        if (JoinNavigations.Count > 0)
        {
            JoinNavigations.Add(joinNavigation);
            return this;
        }
        
        return new LibSqlTable<T>(Provider, Expression, Db, JoinNavigations.Concat(new[] { joinNavigation }).ToList());
    }
    
    public LibSqlTable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
    {
        var leftProperty = navigationPropertyPath.Salama();
        var joinNavigation = GenerateJoinNavigation(leftProperty);
        if (JoinNavigations.Count > 0)
        {
            JoinNavigations.Add(joinNavigation);
            return this;
        }
        
        return new LibSqlTable<T>(Provider, Expression, Db, JoinNavigations.Concat(new[] { joinNavigation }).ToList());
    }
    
    private JoinNavigation GenerateJoinNavigation(PropertyInfo leftProperty)
    {
        // Get the type of the foreign key
        var foreignKeyType = leftProperty.PropertyType;
        if (foreignKeyType.IsGenericType && foreignKeyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            foreignKeyType = foreignKeyType.GetGenericArguments()[0];
        }
        
        // Get ForeignKey attribute
        var foreignKeyAttribute = leftProperty.GetCustomAttribute<ForeignKeyAttribute>();
        if (foreignKeyAttribute == null)
        {
            throw new ArgumentException("The property does not have a ForeignKey attribute.");
        }

        // Make sure the target type actually has the foreign key propert
        var rightProperty = foreignKeyType.GetProperty(foreignKeyAttribute.Name);
        if (rightProperty == null)
        {
            throw new ArgumentException($"The foreign key property '{foreignKeyAttribute.Name}' does not exist on type '{foreignKeyType.Name}'.");
        }
        
        var joinNavigation = new JoinNavigation(
            leftProperty.DeclaringType!,
            foreignKeyType,
            leftProperty,
            rightProperty);
        return joinNavigation;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        LoadAutoIncludes();
        return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        LoadAutoIncludes();
        return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
    }

    private void LoadAutoIncludes()
    {
        LoadAutoIncludesForType(typeof(T));
    }
    
    private void LoadAutoIncludesForType(Type type)
    {
        var autoIncludeProperties = type.GetLibSqlAutoIncludeProperties();
        foreach (var property in autoIncludeProperties)
        {
            var joinNavigation = GenerateJoinNavigation(property.Value);
            // Recursive load
            JoinNavigations.Add(joinNavigation);
            LoadAutoIncludesForType(joinNavigation.RightDataType);
        }
    }
    
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
        var keyValue =  item.GetLibSqlPrimaryKeyProperty()?.GetValue(item);
        if (keyValue == null)
        {
            throw new ArgumentException($"The item does not have a value for the primary key '{PrimaryKey}'.");
        }
        
        var query = QueryBuilder.BuildDeleteQuery(TableName, PrimaryKey.Name, keyValue);
        await Db.Client.QueryAsync(query);
    }
    
    private void AssignLastInsertRowId(T item, PipelineResponse? pipelineResponse)
    {
        var newKey = pipelineResponse?.Results?.FirstOrDefault()?.Response?.Result?.LastInsertRowId;
        if (newKey == null)
        {
            throw new InvalidOperationException("Failed to retrieve the last insert row ID.");
        }
        
        var keyProperty = GetPrimaryKeyProperty();
        if (keyProperty.PropertyType == typeof(int))
        {
            keyProperty.SetValue(item, newKey as int?);
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
    
    private PropertyInfo GetPrimaryKeyProperty() => LibSqlExtensions.GetLibSqlPrimaryKeyProperty(typeof(T));

    /// <summary>
    /// Get the table name from the Table attribute if it exists.
    /// </summary>
    /// <returns>The name of the table.</returns>
    private string GetTableName() => typeof(T).GetLibSqlTableName();

    // ITable implementation
    public string GetName() => TableName;
}