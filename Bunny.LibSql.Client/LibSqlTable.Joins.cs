using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using Bunny.LibSql.Client.Extensions;

namespace Bunny.LibSql.Client;

public partial class LibSqlTable<T>
{
    public LibSqlTable<T> Include<TOther>(Expression<Func<TOther, object>> navigationPropertyPath)
    {
        var leftProperty = navigationPropertyPath.GetSelectedExpressionProperty();
        var joinNavigation = GenerateJoinNavigation(leftProperty);
        if (JoinNavigations.Count > 0)
        {
            JoinNavigations.Add(joinNavigation);
            return this;
        }
        
        return new LibSqlTable<T>(Provider, Expression, Db, JoinNavigations.Concat([joinNavigation]).ToList());
    }
    
    public LibSqlTable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
    {
        var leftProperty = navigationPropertyPath.GetSelectedExpressionProperty();
        var joinNavigation = GenerateJoinNavigation(leftProperty);
        if (JoinNavigations.Count > 0)
        {
            JoinNavigations.Add(joinNavigation);
            return this;
        }
        
        return new LibSqlTable<T>(Provider, Expression, Db, JoinNavigations.Concat([joinNavigation]).ToList());
    }
    
    private void LoadAllAutoIncludes()
    {
        LoadRecursiveAutoIncludesForType(typeof(T));
    }
    
    private void LoadRecursiveAutoIncludesForType(Type type)
    {
        var autoIncludeProperties = type.GetLibSqlAutoIncludeProperties();
        foreach (var property in autoIncludeProperties)
        {
            var joinNavigation = GenerateJoinNavigation(property.Value);
            // Recursive load
            JoinNavigations.Add(joinNavigation);
            LoadRecursiveAutoIncludesForType(joinNavigation.RightDataType);
        }
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
}