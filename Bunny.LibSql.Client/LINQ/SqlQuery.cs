namespace Bunny.LibSql.Client.LINQ;

public record SqlQuery(string sql, object[]? args = null)
{
    public string Sql { get; set; } = sql;
    public object[]? Args { get; set; } = args;
}