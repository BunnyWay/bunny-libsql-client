using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using Bunny.LibSql.Client.Attributes;
using Bunny.LibSql.Client.Extensions;
using Bunny.LibSql.Client.LINQ;

namespace Bunny.LibSql.Client;

public partial class LibSqlTable<T>
{
    public LibSqlTable<T> Include<TOther>(Expression<Func<TOther, object>> navigationPropertyPath)
    {
        var leftProperty = navigationPropertyPath.GetSelectedExpressionProperty();
        if (leftProperty.GetCustomAttribute<AutoIncludeAttribute>() != null)
        {
            return this;
        }
        
        var joinNavigations = GenerateJoinNavigations(leftProperty);
        if (JoinNavigations.Count > 0)
        {
            foreach (var joinNavigation in joinNavigations)
            {
                JoinNavigations.Add(joinNavigation);
            }
            return this;
        }
        
        return new LibSqlTable<T>(Provider, Expression, Db, JoinNavigations.Concat(joinNavigations).ToList());
    }
    
    public LibSqlTable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
    {
        var leftProperty = navigationPropertyPath.GetSelectedExpressionProperty();
        if (leftProperty.GetCustomAttribute<AutoIncludeAttribute>() != null)
        {
            return this;
        }
        
        var joinNavigations = GenerateJoinNavigations(leftProperty);
        if (JoinNavigations.Count > 0)
        {
            foreach (var joinNavigation in joinNavigations)
            {
                JoinNavigations.Add(joinNavigation);
            }
            return this;
        }
        
        return new LibSqlTable<T>(Provider, Expression, Db, JoinNavigations.Concat(joinNavigations).ToList());
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
            var joinNavigations = GenerateJoinNavigations(property.Value);
            foreach (var joinNavigation in joinNavigations)
            {
                JoinNavigations.Add(joinNavigation);
                LoadRecursiveAutoIncludesForType(joinNavigation.RightDataType);
            }
        }
    }
    
    private IEnumerable<JoinNavigation> GenerateJoinNavigations(PropertyInfo leftProperty)
    {
        // Get the type of the foreign key
        var rightModelType = leftProperty.PropertyType;
        if (rightModelType.IsGenericType && rightModelType.GetGenericTypeDefinition() == typeof(List<>))
        {
            rightModelType = rightModelType.GetGenericArguments()[0];
        }
        var leftModelType = leftProperty.DeclaringType!;
        
        
        // Get ForeignKey attribute
        var manyToManyAttribute = leftProperty.GetCustomAttribute<ManyToManyAttribute>();
        if (manyToManyAttribute != null)
        { 
            var joinNavigations = GenerateJoinManyToMany(leftProperty, leftModelType, rightModelType, manyToManyAttribute);
            foreach (var joinNavigation in joinNavigations)
            {
                yield return joinNavigation;
            }
            yield break;
        }
        else
        {
            yield return GenerateJoinForeignKey(leftProperty, leftModelType, rightModelType);
        }
    }

    private IEnumerable<JoinNavigation> GenerateJoinManyToMany(PropertyInfo leftProperty, Type leftModelType, Type rightModelType, ManyToManyAttribute manyToManyAttribute)
    {
        var connectorTableProperty = this.Db.GetDatabasePropertyForType(manyToManyAttribute.ConnectionModelType);
        if (connectorTableProperty == null)
        {
            throw new ArgumentException("The property does not have a valid join attribute.");
        }
        
        var rightType = connectorTableProperty.PropertyType.GetGenericArguments().First();
        var finalRightType = leftProperty.PropertyType.GetGenericArguments().First();
        
        var connectorTableLeftProperty = manyToManyAttribute.ConnectionModelType.GetProperties()?
            .Where(e => e.GetCustomAttribute<ForeignKeyForAttribute>()?.Type == leftModelType).FirstOrDefault();
        var connectorTableRightProperty = manyToManyAttribute.ConnectionModelType.GetProperties()?
            .Where(e => e.GetCustomAttribute<ForeignKeyForAttribute>()?.Type == finalRightType).FirstOrDefault();
        if (connectorTableLeftProperty == null || connectorTableRightProperty == null)
        {
            throw new ArgumentException("Can not find connecting ManyToManyKey attributes on the connection table");
        }
        
        // We connect the left property to the connector table
        var connectorJoin = new JoinNavigation(
            leftModelType,
            rightType,
            LibSqlExtensions.GetLibSqlPrimaryKeyProperty(leftModelType),
            connectorTableLeftProperty, 
            null);
        yield return connectorJoin;

        
        var finalRightTypePrimaryKeyProperty = finalRightType.GetLibSqlPrimaryKeyProperty();
        // We join the final table, and connect the result to the main item
        var joinNavigation = new JoinNavigation(
            rightType,
            finalRightType,
            connectorTableRightProperty,
            finalRightTypePrimaryKeyProperty,
            leftProperty);
        yield return joinNavigation;
    }
    
    private JoinNavigation GenerateJoinForeignKey(PropertyInfo leftProperty, Type leftModelType, Type rightModelType)
    {
        var rightProperty = rightModelType.GetProperties()?
            .Where(e => e.GetCustomAttribute<ForeignKeyForAttribute>()?.Type == leftModelType).FirstOrDefault();
        
        var joinNavigation = new JoinNavigation(
            leftModelType,
            rightModelType,
            this.GetPrimaryKeyProperty(),
            rightProperty,
            leftProperty);
        return joinNavigation;
    }
    
}