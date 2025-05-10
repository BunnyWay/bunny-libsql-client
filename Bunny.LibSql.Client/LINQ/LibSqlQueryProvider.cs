using System.Linq.Expressions;

namespace Bunny.LibSql.Client.LINQ;

public class LibSqlQueryProvider<T> : IQueryProvider
{
    public LibSqlTable<T> Table { get; set; }
    public LibSqlQueryProvider(LibSqlTable<T> table)
    {
        this.Table = table;
    }
    
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var visitor = new LinqToSqliteVisitor(Table.JoinNavigations);
        var query = visitor.Translate(expression);
        return new LibSqlTable<TElement>(this, expression, Table.Db);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        var visitor = new LinqToSqliteVisitor(Table.JoinNavigations);
        var query = visitor.Translate(expression);

        var test = new List<T>();
        var results = Table.Db.Client.QueryAsync<T>(query.Sql, query.Parameters, Table.JoinNavigations).Result;
        foreach (var result in results)
        {
            test.Add(result);
        }
        
        if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return (TResult)(object)test.AsEnumerable();
        }

        return (TResult)(object)test.FirstOrDefault();
    }
    
    // We don't implement generics
    public IQueryable CreateQuery(Expression expression) => throw new NotImplementedException();
    public object? Execute(Expression expression) => throw new NotImplementedException();
}