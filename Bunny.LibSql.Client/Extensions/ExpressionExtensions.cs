using System.Linq.Expressions;
using System.Reflection;

namespace Bunny.LibSql.Client.Extensions;

public static class ExpressionExtensions
{
    public static PropertyInfo GetSelectedExpressionProperty<T, TProperty>(this Expression<Func<T, TProperty>> navigationPropertyPath)
    {
        var memberExpression = navigationPropertyPath.Body as MemberExpression;
        if (memberExpression == null)
        {
            throw new ArgumentException("The expression is not a member expression.");
        }
        var leftProperty = typeof(T).GetProperty(memberExpression.Member.Name);
        if(leftProperty == null)
        {
            throw new ArgumentException($"The property '{nameof(memberExpression.Member.Name)}' does not exist on type '{typeof(T).Name}'.");
        }

        return leftProperty;
    }

    public static PropertyInfo GetSelectedExpressionProperty<TOther>(this Expression<Func<TOther, object>> navigationPropertyPath)
    {
        var memberExpression = navigationPropertyPath.Body as MemberExpression;
        if (memberExpression == null)
        {
            throw new ArgumentException("The expression is not a member expression.");
        }
        var leftProperty = typeof(TOther).GetProperty(memberExpression.Member.Name);
        if(leftProperty == null)
        {
            throw new ArgumentException($"The property '{nameof(memberExpression.Member.Name)}' does not exist on type '{typeof(TOther).Name}'.");
        }

        return leftProperty;
    }
}