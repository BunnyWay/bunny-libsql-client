using System.Reflection;

namespace Bunny.LibSql.Client.LINQ;

public class JoinNavigation(
    Type leftDataType,
    Type rightDataType,
    PropertyInfo leftProperty,
    PropertyInfo rightProperty,
    PropertyInfo? dataProperty)
{
    public Type LeftDataType { get; } = leftDataType;
    public PropertyInfo LeftProperty { get; } = leftProperty;
    public Type RightDataType { get; } = rightDataType;
    public PropertyInfo RightProperty { get; } = rightProperty;
    public PropertyInfo? DataProperty { get; } = dataProperty;
    public bool DataPropertyIsList { get; private set; } = 
        dataProperty != null && dataProperty.PropertyType.GetGenericArguments().Any();
}