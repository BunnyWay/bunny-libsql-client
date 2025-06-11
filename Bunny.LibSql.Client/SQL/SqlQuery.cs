namespace Bunny.LibSql.Client.SQL;

public record SqlQuery(string sql, object[]? args = null)
{
    public string SqlCommand { get; } = sql;
    public object[]? Args { get; } = args;
}