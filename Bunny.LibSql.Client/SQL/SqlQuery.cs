namespace Bunny.LibSql.Client.SQL;

public record SqlQuery(string sql, object[]? args = null)
{
    public string Sql { get; set; } = sql;
    public object[]? Args { get; set; } = args;
}