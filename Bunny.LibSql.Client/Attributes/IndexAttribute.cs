namespace Bunny.LibSql.Client.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class IndexAttribute : Attribute
{
    public string Name   { get; }
    public bool   Unique { get; set; }

    public IndexAttribute(string name = null)
        => Name = name;
}