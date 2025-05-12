namespace Bunny.LibSql.Client.Attributes;

public class ManyToManyAttribute : Attribute
{
    public Type ConnectionModelType { get; set; }
    public ManyToManyAttribute(Type connectionModelType)
    {
        ConnectionModelType = connectionModelType;
    }
}