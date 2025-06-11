using System.Linq.Expressions;

namespace Bunny.LibSql.Client.LINQ;

public interface IAsyncQueryProvider : IQueryProvider
{
    Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken);
}