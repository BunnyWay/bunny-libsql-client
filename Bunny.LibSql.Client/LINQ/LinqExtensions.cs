using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Bunny.LibSql.Client.Types;

namespace Bunny.LibSql.Client.LINQ;

public static class LinqExtensions
{
    public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
    {
        if (source.Provider is IAsyncQueryProvider asyncProvider)
            return asyncProvider.ExecuteAsync<List<TSource>>(source.Expression, cancellationToken);

        // fallback to synchronous
        return Task.FromResult(source.ToList());
    }
    
    public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
    {
        if (source.Provider is IAsyncQueryProvider asyncProvider)
        {
            var callExpr = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.FirstOrDefault),
                [typeof(TSource)],
                source.Expression
            );
            
            return asyncProvider.ExecuteAsync<TSource>(callExpr, cancellationToken);
        }

        // fallback to synchronous
        return Task.FromResult(source.FirstOrDefault());
    }
    
    public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
    {
        if (source.Provider is IAsyncQueryProvider asyncProvider)
        {
            var callExpr = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.First),
                [typeof(TSource)],
                source.Expression
            );

            return asyncProvider.ExecuteAsync<TSource>(callExpr, cancellationToken);
        }

        // fallback to synchronous
        return Task.FromResult(source.First());
    }
    
    // This will create an ambiguity with the other Count method, but we want that, so we can force the user to use the async version
    public static long Count<TSource>(this IQueryable<TSource> source)
    {
        return source.CountAsync().GetAwaiter().GetResult();
    }
    
    public static async Task<long> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
    {
        if (source.Provider is IAsyncQueryProvider asyncProvider)
        {
            var callExpr = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Count),
                [typeof(TSource)],
                source.Expression
            );

            var test = await asyncProvider.ExecuteAsync<long>(callExpr, cancellationToken);
            return test;
        }
        
        throw new NotSupportedException();
    }
    
    /// <summary>
    /// Asynchronously computes the sum of a sequence of numeric values obtained by invoking a projection function.
    /// </summary>
    public static async Task<TResult> SumAsync<TSource, TResult>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        if (source.Provider is IAsyncQueryProvider asyncProvider)
        {
            // Build the Queryable.Sum<TSource, TResult>(source, selector) call expression
            var callExpr = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Sum),
                new[] { typeof(TSource) },
                source.Expression,
                Expression.Quote(selector)
            );

            return await asyncProvider.ExecuteAsync<TResult>(callExpr, cancellationToken);
        }

        throw new NotSupportedException();
    }
}