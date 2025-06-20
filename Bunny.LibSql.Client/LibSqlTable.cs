using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Bunny.LibSql.Client.LINQ;

namespace Bunny.LibSql.Client;

// TODO: explore whether we can add a "query tracker" to LINQ to get metrics for the exact queries etc.
public partial class LibSqlTable<T> : IQueryable<T>, IOrderedQueryable<T>
{
    public readonly string TableName;
    public readonly PropertyInfo PrimaryKeyProperty;
    public Type ElementType => typeof(T);
    public Expression Expression { get; }
    public IQueryProvider Provider { get; }
    public LibSqlDbContext Db { get; }
    public List<JoinNavigation> JoinNavigations { get; } = [];
    public bool AutoValidateEntities { get; set; }
    
    
    #region Constructors
    // This is needed by reflection
    public LibSqlTable(LibSqlDbContext db)
    {
        Db = db;
        TableName = GetTableName();
        PrimaryKeyProperty = GetPrimaryKeyProperty();
        Provider = new LibSqlQueryProvider<T>(this);
        Expression = Expression.Constant(this);
    }
    public LibSqlTable(IQueryProvider provider, Expression expression, LibSqlDbContext db)
    {
        Db = db;
        Provider = provider;
        Expression = expression;
        Provider = new LibSqlQueryProvider<T>(this);
        TableName = GetTableName();
    }
    public LibSqlTable(IQueryProvider provider, Expression expression, LibSqlDbContext db, List<JoinNavigation> joinNavigations)
    {
        Db = db;
        Provider = provider;
        Expression = expression;
        TableName = GetTableName();
        JoinNavigations = joinNavigations;
        Provider = new LibSqlQueryProvider<T>(this);
    }
    #endregion
    
    public IEnumerator<T> GetEnumerator()
    {
        LoadAllAutoIncludes();
        return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        LoadAllAutoIncludes();
        return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
    }

    public async Task TruncateTable()
    {
        await Db.Client.QueryAsync($"DELETE FROM {TableName}");
    }
    
    #region Helpers
    private PropertyInfo GetPrimaryKeyProperty() => LibSqlExtensions.GetLibSqlPrimaryKeyProperty(typeof(T));

    /// <summary>
    /// Get the table name from the Table attribute if it exists.
    /// </summary>
    /// <returns>The name of the table.</returns>
    private string GetTableName() => typeof(T).GetLibSqlTableName();
    #endregion
}