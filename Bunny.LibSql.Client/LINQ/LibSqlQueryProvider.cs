using System.Linq.Expressions;

namespace Bunny.LibSql.Client.LINQ;

public class LibSqlQueryProvider<T> : IQueryProvider
{
    private LibSqlTable<T> _table;
    public LibSqlQueryProvider(LibSqlTable<T> table)
    {
        _table = table;
    }
    
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var visitor = new LinqToSqliteVisitor(_table.JoinNavigations);
        var query = visitor.Translate(expression);
        return new LibSqlTable<TElement>(this, expression, _table.Db);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        var visitor = new LinqToSqliteVisitor(_table.JoinNavigations);
        var query = visitor.Translate(expression);
        // TODO: convert to async?
        var clientResults = _table.Db.Client.QueryAsync<T>(query.Sql, query.Parameters, _table.JoinNavigations).Result;
        
        var results = new List<T>(clientResults.Count);
        foreach (var result in clientResults)
        {
            results.Add(result);
        }
        
        if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return (TResult)(object)results.AsEnumerable();
        }

        return (TResult)(object)results.FirstOrDefault()!;
    }
    
    // We don't implement generics
    public IQueryable CreateQuery(Expression expression) => throw new NotImplementedException();
    public object? Execute(Expression expression) => throw new NotImplementedException();
}