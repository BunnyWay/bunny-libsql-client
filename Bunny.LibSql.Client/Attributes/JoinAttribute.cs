namespace Bunny.LibSql.Client.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class JoinAttribute : Attribute
{
    /// <summary>
    /// Child‐table’s FK column name.
    /// </summary>
    public string ForeignKey { get; }
    public JoinAttribute(string foreignKey)
        => ForeignKey = foreignKey;
}