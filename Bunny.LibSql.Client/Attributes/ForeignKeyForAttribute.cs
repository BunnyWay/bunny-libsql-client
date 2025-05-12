namespace Bunny.LibSql.Client.Attributes;

public class ForeignKeyForAttribute : Attribute
{
    public Type Type { get; }
    public ForeignKeyForAttribute(Type type)
    {
        Type = type;
    }
}