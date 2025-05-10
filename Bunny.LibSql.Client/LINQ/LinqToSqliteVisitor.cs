using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Bunny.LibSql.Client.LINQ;


public class LinqToSqliteVisitor : ExpressionVisitor
{
    private StringBuilder _sqlBuilder;
    private List<object> _parameters;
    private int _skip;
    private int _take = -1; // -1 means no limit
    private List<string> _orderByClauses;
    private Dictionary<string, string> _columnAliases; // For handling Select projections
    private List<JoinNavigation> _joinNavigations;
    
    public LinqToSqliteVisitor(List<JoinNavigation> joinNavigations)
    {
        _sqlBuilder = new StringBuilder();
        _parameters = new List<object>();
        _orderByClauses = new List<string>();
        _columnAliases = new Dictionary<string, string>();
        _joinNavigations = joinNavigations;
    }

    public (string Sql, IEnumerable<object> Parameters) Translate(Expression expression)
    {
        _sqlBuilder.Clear();
        _parameters.Clear();
        _orderByClauses.Clear();
        _columnAliases.Clear();
        _skip = 0;
        _take = -1;

        Visit(expression);

        // Build SELECT clause
        var finalSql = new StringBuilder();
        finalSql.Append("SELECT ");

        if (_columnAliases.Any())
        {
            finalSql.Append(string.Join(", ", _columnAliases.Select(kvp => $"{kvp.Key} AS {kvp.Value}")));
        }
        else
        {
            var tableName = GetTableName(expression);
            if (!string.IsNullOrEmpty(tableName))
                finalSql.Append($"{tableName}.*");
            else
                finalSql.Append("*");
            
            foreach (var join in _joinNavigations)
            {
                finalSql.Append($", {join.RightDataType.GetLibSqlTableName()}.*");
            }
        }

        // FROM clause with LEFT JOINs
        var mainTable = GetTableName(expression) ?? "YourTable";
        finalSql.Append(" FROM ");
        finalSql.Append(mainTable);

        // Append LEFT JOIN clauses for each navigation
        foreach (var join in _joinNavigations)
        {
            // Assumes join.MemberName matches table name and primary key is 'Id'
            finalSql.Append(" LEFT JOIN ");
            finalSql.Append(join.RightDataType.GetLibSqlTableName());
            finalSql.Append(" ON ");
            finalSql.Append($"{join.LeftDataType.GetLibSqlTableName()}.{join.LeftDataType.GetLibSqlPrimaryKeyProperty().Name} = {join.RightDataType.GetLibSqlTableName()}.{join.RightProperty.Name}");
        }

        // WHERE clause
        if (_sqlBuilder.Length > 0)
        {
            finalSql.Append(" WHERE ");
            finalSql.Append(_sqlBuilder.ToString());
        }

        // ORDER BY
        if (_orderByClauses.Any())
        {
            finalSql.Append(" ORDER BY ");
            finalSql.Append(string.Join(", ", _orderByClauses));
        }

        // LIMIT & OFFSET
        if (_take > -1)
        {
            finalSql.Append(" LIMIT ");
            finalSql.Append(_take);
        }
        if (_skip > 0)
        {
            if (_take == -1)
                finalSql.Append(" LIMIT -1");
            finalSql.Append(" OFFSET ");
            finalSql.Append(_skip);
        }

        return (finalSql.ToString().TrimEnd() + ";", _parameters);
    }

    private string GetTableName(Expression expression)
    {
        if (expression is MethodCallExpression methodCall)
        {
            // If it's a Queryable method, the source is often the first argument
            if (methodCall.Arguments.Count > 0)
            {
                return GetTableName(methodCall.Arguments[0]);
            }
        }
        else if (expression is ConstantExpression constant && constant.Value is IQueryable queryable)
        {
            // This is a simplistic way; real-world scenarios might involve reflection
            // or a dedicated mechanism to resolve entity types to table names.
            return queryable.ElementType.Name;
        }
        // Add more sophisticated table name resolution as needed
        return null;
    }


    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Handle Where
        if (node.Method.Name == "Where" && node.Method.DeclaringType == typeof(Queryable))
        {
            Visit(node.Arguments[0]); // Visit the source
            if (_sqlBuilder.Length > 0) // If there's an existing WHERE clause
            {
                _sqlBuilder.Append(" AND ");
            }
            var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
            Visit(lambda.Body);
            return node; // Indicate we've handled it
        }
        // Handle Select (basic projection to aliased columns)
        else if (node.Method.Name == "Select" && node.Method.DeclaringType == typeof(Queryable))
        {
            Visit(node.Arguments[0]); // Visit the source
            var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);

            if (lambda.Body is NewExpression newExp) // Handles anonymous types: new { A = x.ColA, B = x.ColB }
            {
                for (int i = 0; i < newExp.Members.Count; i++)
                {
                    var member = newExp.Members[i];
                    var argument = newExp.Arguments[i];
                    if (argument is MemberExpression memberExpArg)
                    {
                         _columnAliases[GetColumnName(memberExpArg)] = member.Name;
                    }
                    else
                    {
                        // More complex projections would need deeper handling
                        throw new NotSupportedException($"Unsupported projection argument: {argument.NodeType}");
                    }
                }
            }
            else if (lambda.Body is MemberExpression memberExp) // Handles single member selection: x => x.ColA
            {
                 _columnAliases[GetColumnName(memberExp)] = memberExp.Member.Name; // Or a preferred alias
            }
            else
            {
                throw new NotSupportedException($"Unsupported select expression: {lambda.Body.NodeType}");
            }
            return node;
        }
        // Handle First / FirstOrDefault (with optional predicate)
        else if ((node.Method.Name == "First" || node.Method.Name == "FirstOrDefault")
                 && node.Method.DeclaringType == typeof(Queryable))
        {
            // 1) Visit the source (e.g. the IQueryable)
            Visit(node.Arguments[0]);

            // 2) If there's a predicate overload, apply it to the WHERE clause
            if (node.Arguments.Count == 2)
            {
                if (_sqlBuilder.Length > 0)
                    _sqlBuilder.Append(" AND ");

                var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
                Visit(lambda.Body);
            }

            // 3) Impose LIMIT 1
            _take = 1;

            return node;
        }
        // Handle OrderBy and OrderByDescending
        else if ((node.Method.Name == "OrderBy" || node.Method.Name == "OrderByDescending") && node.Method.DeclaringType == typeof(Queryable))
        {
            Visit(node.Arguments[0]); // Visit the source
            var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
            var memberExpression = lambda.Body as MemberExpression;
            if (memberExpression == null && lambda.Body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                memberExpression = unary.Operand as MemberExpression; // Handle cases like x => (object)x.Id
            }

            if (memberExpression != null)
            {
                _orderByClauses.Add($"{GetColumnName(memberExpression)} {(node.Method.Name == "OrderByDescending" ? "DESC" : "ASC")}");
            }
            else
            {
                throw new NotSupportedException("OrderBy clause must be on a direct member.");
            }
            return node;
        }
        // Handle ThenBy and ThenByDescending
        else if ((node.Method.Name == "ThenBy" || node.Method.Name == "ThenByDescending") && node.Method.DeclaringType == typeof(Queryable))
        {
            // We assume OrderBy was called first, so the source is already visited.
            // If not, this would need more complex handling to ensure correct order of visitation.
            var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
             var memberExpression = lambda.Body as MemberExpression;
            if (memberExpression == null && lambda.Body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                memberExpression = unary.Operand as MemberExpression;
            }

            if (memberExpression != null)
            {
                _orderByClauses.Add($"{GetColumnName(memberExpression)} {(node.Method.Name == "ThenByDescending" ? "DESC" : "ASC")}");
            }
            else
            {
                throw new NotSupportedException("ThenBy clause must be on a direct member.");
            }
            return node;
        }
        // Handle Take
        else if (node.Method.Name == "Take" && node.Method.DeclaringType == typeof(Queryable))
        {
            Visit(node.Arguments[0]); // Visit the source
            if (node.Arguments[1] is ConstantExpression constant)
            {
                _take = (int)constant.Value;
            }
            else
            {
                throw new NotSupportedException("Take argument must be a constant.");
            }
            return node;
        }
        // Handle Skip
        else if (node.Method.Name == "Skip" && node.Method.DeclaringType == typeof(Queryable))
        {
            Visit(node.Arguments[0]); // Visit the source
            if (node.Arguments[1] is ConstantExpression constant)
            {
                _skip = (int)constant.Value;
            }
            else
            {
                throw new NotSupportedException("Skip argument must be a constant.");
            }
            return node;
        }
        // Handle string methods like Contains, StartsWith, EndsWith
        else if (node.Method.DeclaringType == typeof(string))
        {
            // Ensure the object of the method call is a MemberExpression (column)
            if (node.Object is MemberExpression memberExpr)
            {
                Visit(memberExpr); // Column name

                switch (node.Method.Name)
                {
                    case "Contains":
                        _sqlBuilder.Append(" LIKE ?");
                        AddParameter($"%{GetExpressionValue(node.Arguments[0])}%");
                        break;
                    case "StartsWith":
                        _sqlBuilder.Append(" LIKE ?");
                        AddParameter($"{GetExpressionValue(node.Arguments[0])}%");
                        break;
                    case "EndsWith":
                        _sqlBuilder.Append(" LIKE ?");
                        AddParameter($"%{GetExpressionValue(node.Arguments[0])}");
                        break;
                    default:
                        throw new NotSupportedException($"String method {node.Method.Name} is not supported.");
                }
                return node; // Handled
            }
        }
        // Handle Enumerable.Contains for "IN" clauses (e.g., list.Contains(x.Column))
        else if (node.Method.Name == "Contains" &&
                 node.Method.DeclaringType == typeof(Enumerable) &&
                 node.Arguments.Count == 2)
        {
            Visit(node.Arguments[1]); // This should be the column (e.g., x.Column)
            _sqlBuilder.Append(" IN (");

            var listExpression = node.Arguments[0];
            var list = GetExpressionValue(listExpression) as IEnumerable;
            if (list == null)
            {
                throw new NotSupportedException("Contains argument must be a collection.");
            }

            bool first = true;
            foreach (var item in list)
            {
                if (!first) _sqlBuilder.Append(", ");
                _sqlBuilder.Append("?");
                AddParameter(item);
                first = false;
            }
            _sqlBuilder.Append(")");
            return node; // Handled
        }


        // Fallback or throw for unhandled methods
        // In a real provider, you might visit arguments if it's a sub-query or part of the projection
        return base.VisitMethodCall(node); // Or throw new NotSupportedException($"Method {node.Method.Name} not supported.");
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        _sqlBuilder.Append("(");
        Visit(node.Left);

        switch (node.NodeType)
        {
            case ExpressionType.AndAlso:
                _sqlBuilder.Append(" AND ");
                break;
            case ExpressionType.OrElse:
                _sqlBuilder.Append(" OR ");
                break;
            case ExpressionType.Equal:
                _sqlBuilder.Append(" = ");
                break;
            case ExpressionType.NotEqual:
                _sqlBuilder.Append(" != ");
                break;
            case ExpressionType.LessThan:
                _sqlBuilder.Append(" < ");
                break;
            case ExpressionType.LessThanOrEqual:
                _sqlBuilder.Append(" <= ");
                break;
            case ExpressionType.GreaterThan:
                _sqlBuilder.Append(" > ");
                break;
            case ExpressionType.GreaterThanOrEqual:
                _sqlBuilder.Append(" >= ");
                break;
            case ExpressionType.Add:
                 _sqlBuilder.Append(" + ");
                 break;
            case ExpressionType.Subtract:
                 _sqlBuilder.Append(" - ");
                 break;
            case ExpressionType.Multiply:
                 _sqlBuilder.Append(" * ");
                 break;
            case ExpressionType.Divide:
                 _sqlBuilder.Append(" / ");
                 break;
            default:
                throw new NotSupportedException($"Binary operator {node.NodeType} not supported.");
        }

        Visit(node.Right);
        _sqlBuilder.Append(")");
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // This is where you'd map CLR properties to database column names
        // For simplicity, we'll assume the member name is the column name.
        // In a real system, you might use attributes or a mapping dictionary.
        if (node.Expression != null && node.Expression.NodeType == ExpressionType.Parameter)
        {
            // It's a column access, e.g., x.Name
            _sqlBuilder.Append(GetColumnName(node));
            return node;
        }
        else if (node.Expression != null && node.Expression.NodeType == ExpressionType.Constant)
        {
            // It's a captured variable or a static member, treat as a constant.
            // This is needed for cases like: string name = "Test"; query.Where(x => x.Name == name);
            AddParameter(GetExpressionValue(node));
            _sqlBuilder.Append("?");
            return node;
        }
        // Could also be a member access on a captured variable, e.g., criteria.Name
        // where criteria is an object.
        throw new NotSupportedException($"Member access for {node.Member.Name} on type {node.Expression?.NodeType} is not supported in this context.");
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value == null)
        {
            _sqlBuilder.Append("NULL");
        }
        // Avoid adding parameters for IQueryable sources in the FROM clause or for method calls.
        // This needs refinement to be more context-aware.
        else if (!(node.Value is IQueryable) && !IsPartOfMethodCallSource(node))
        {
            AddParameter(node.Value);
            _sqlBuilder.Append("?");
        }
        return node;
    }

    // Helper to determine if a constant is the source of a queryable method call
    private bool IsPartOfMethodCallSource(ConstantExpression constantNode)
    {
        // This is a very basic check. A more robust solution would involve tracking the expression stack.
        // The goal is to prevent the IQueryable source itself (like `dbContext.Users`) from being parameterized.
        // This logic might need to be more sophisticated depending on query complexity.
        return constantNode.Value is IQueryable;
    }


    protected override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Quote:
                // just unwrap the lambda (or whatever is inside) and keep going
                return Visit(node.Operand);

            case ExpressionType.Not:
                _sqlBuilder.Append("NOT (");
                Visit(node.Operand);
                _sqlBuilder.Append(")");
                return node;

            case ExpressionType.Convert:
                // ignore casts
                Visit(node.Operand);
                return node;

            default:
                throw new NotSupportedException($"Unary operator {node.NodeType} not supported.");
        }
    }

    // Helper to strip an ExpressionType.Quote if present (used with LambdaExpressions from IQueryable methods)
    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }
        return e;
    }

    private void AddParameter(object value)
    {
        _parameters.Add(value);
    }

    private string GetColumnName(MemberExpression memberExpression)
    {
        // Basic implementation: uses member name.
        // Could be extended with attributes for custom column names.
        // e.g., [Column("user_name")] public string UserName { get; set; }
        var member = memberExpression.Member;
        // var columnAttribute = member.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>();
        // if (columnAttribute != null) return columnAttribute.Name;
        return member.Name;
    }

    private object GetExpressionValue(Expression expression)
    {
        // If it's a constant, just return its value
        if (expression is ConstantExpression constExp)
        {
            return constExp.Value;
        }
        // If it's a member access on a constant (e.g., a captured variable's field/property)
        else if (expression is MemberExpression memberExp && memberExp.Expression is ConstantExpression parentConstExp)
        {
            var parentValue = parentConstExp.Value;
            if (memberExp.Member is FieldInfo field)
            {
                return field.GetValue(parentValue);
            }
            if (memberExp.Member is PropertyInfo prop)
            {
                return prop.GetValue(parentValue);
            }
        }
        // For other cases, compile and invoke the expression (can be slow, use with caution)
        // This is often needed for arguments to methods like StartsWith, Contains, etc.
        // or for accessing elements of a collection.
        try
        {
             // Create a lambda expression of type Func<object> to get the value.
            var objectMember = Expression.Convert(expression, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            return getterLambda.Compile()();
        }
        catch (Exception ex)
        {
             throw new NotSupportedException($"Could not get value from expression: {expression}. Error: {ex.Message}");
        }
    }
}