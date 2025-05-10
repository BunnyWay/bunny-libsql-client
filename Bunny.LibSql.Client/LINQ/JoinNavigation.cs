using System.Reflection;

namespace Bunny.LibSql.Client;

public class JoinNavigation(
    Type leftDataType,
    Type rightDataType,
    PropertyInfo leftProperty,
    PropertyInfo rightProperty)
{
    public Type LeftDataType { get; set; } = leftDataType;
    public PropertyInfo LeftProperty { get; set; } = leftProperty;
    
    public Type RightDataType { get; set; } = rightDataType;
    public PropertyInfo RightProperty { get; set; } = rightProperty;
}

/*
public class JoinNavigation(Type leftTableType, Type rightTableType, PropertyInfo leftProperty, string leftTableName, string leftTableKey, string rightTableName, PropertyInfo rightTableKeyProperty)
{
    public Type LeftTableType { get; set; } = leftTableType;
    public PropertyInfo LeftProperty { get; set; } = leftProperty;
    public string LeftTableName { get; set; } = leftTableName;
    public string LeftTableKey { get; set; } = leftTableKey;
    
    
    public Type RightTableType { get; set; } = rightTableType;
    public string RightTableName { get; set; } = rightTableName;
    public PropertyInfo RightTableKeyProperty { get; set; } = rightTableKeyProperty;
}*/