using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Bunny.LibSql.Client.Attributes;

namespace Bunny.LibSql.Client;

public static class LibSqlExtensions
{
    private static readonly HashSet<string> _defaultPrimaryKeyColumns = new (StringComparer.OrdinalIgnoreCase)
    {
        "Id", "Key", "ID", "KeyId", "Key_ID"
    };

    #region Cache Dictionaries
    [ThreadStatic]
    private static Dictionary<Type, string>? _cachedTableNames = [];
    
    [ThreadStatic]
    private static Dictionary<Type, PropertyInfo>? _cachedPrimaryKeys = [];
    
    [ThreadStatic]
    private static Dictionary<Type, Dictionary<string, PropertyInfo>>? _cachedAutoIncludeProperties = [];
    
    [ThreadStatic]
    private static Dictionary<Type, Dictionary<string, PropertyInfo>>? _cachedMappableProperties = [];
    #endregion
    
    public static PropertyInfo GetLibSqlPrimaryKeyProperty(this object item) => GetLibSqlPrimaryKeyProperty(item.GetType());

    public static PropertyInfo GetLibSqlPrimaryKeyProperty(this Type type)
    {
        // ThreadStatic optimization requirement
        _cachedPrimaryKeys ??= new Dictionary<Type, PropertyInfo>();
        if (_cachedPrimaryKeys.TryGetValue(type, out var primaryKey))
        {
            return primaryKey;
        }

        var properties = type.GetProperties();
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<KeyAttribute>();
            if (attribute != null)
            {
                _cachedPrimaryKeys[type] = property;
                return property;
            }
        }

        // Check against _defaultPrimaryKeyColumns
        foreach (var property in properties)
        {
            if (_defaultPrimaryKeyColumns.Contains(property.Name))
            {
                _cachedPrimaryKeys[type] = property;
                return property;
            }
        }

        throw new InvalidOperationException(
            $"No primary key found for type {type.Name}. Please use the [Key] attribute or ensure the property name is one of the default primary key names.");
    }

    public static Dictionary<string, PropertyInfo> GetLibSqlAutoIncludeProperties(this Type type)
    {
        // ThreadStatic optimization requirement
        _cachedAutoIncludeProperties ??= new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        if (_cachedAutoIncludeProperties.TryGetValue(type, out var props))
        {
            return props;
        }
        
        props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => 
                p.CanRead && 
                p.CanWrite && 
                !p.PropertyType.IsLibSqlSupportedType() && 
                p.GetCustomAttribute<AutoIncludeAttribute>() != null
            )
            .ToDictionary(p => p.Name, p => p);

        _cachedAutoIncludeProperties[type] = props;
        return props;
    }

    public static Dictionary<string, PropertyInfo> GetLibSqlMappableProperties(this Type type)
    {
        // ThreadStatic optimization requirement
        _cachedMappableProperties ??= new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        if (_cachedMappableProperties.TryGetValue(type, out var props))
        {
            return props;
        }
        
        props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.PropertyType.IsLibSqlSupportedType())
            .ToDictionary(p => p.Name, p => p);

        _cachedMappableProperties[type] = props;
        return props;
    }
    
    public static string GetLibSqlTableName(this Type type)
    {
        // ThreadStatic optimization requirement
        _cachedTableNames ??= new Dictionary<Type, string>();
        if (_cachedTableNames.TryGetValue(type, out var tableName))
        {
            return tableName;
        }
        
        var tableAttribute = type.GetCustomAttribute<TableAttribute>();
        if (tableAttribute != null)
        {
            _cachedTableNames[type] = tableAttribute.Name;
            return tableAttribute.Name;
        }
        
        // If no Table attribute is found, use the class name as the table name
        _cachedTableNames[type] = type.Name;
        return type.Name;
    }
}