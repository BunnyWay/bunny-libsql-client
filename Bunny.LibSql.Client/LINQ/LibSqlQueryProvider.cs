using System.Linq.Expressions;

namespace Bunny.LibSql.Client.LINQ;

public class LibSqlQueryProvider<T> : IAsyncQueryProvider
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
        return ExecuteAsync<TResult>(expression, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }
    
    public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var type = typeof(TResult);
        if (type == typeof(long) || type == typeof(int) || type == typeof(double) || type == typeof(float))
        {
            var visitor = new LinqToSqliteVisitor(_table.JoinNavigations);
            var query = visitor.Translate(expression);
            var clientResults = await _table.Db.Client.ExecuteScalarAsync<TResult>(query.Sql, query.Parameters, cancellationToken);
            return clientResults;
        }
        else
        {
            var visitor = new LinqToSqliteVisitor(_table.JoinNavigations);
            var query = visitor.Translate(expression);
            var clientResults = await _table.Db.Client.QueryAsync<T>(query.Sql, query.Parameters, _table.JoinNavigations, cancellationToken);
        
            var results = new List<T>(clientResults.Count);
            foreach (var result in clientResults)
            {
                results.Add(result);
            }

            if (type.GetGenericArguments().Any())
            {
                return (TResult)(object)results.AsEnumerable();
            }

            return (TResult)(object)results.FirstOrDefault()!;
        }
    }

    // We don't implement generics
    public IQueryable CreateQuery(Expression expression) => throw new NotImplementedException();
    public object? Execute(Expression expression) => throw new NotImplementedException();
}