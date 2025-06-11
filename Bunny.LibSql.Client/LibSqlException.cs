namespace Bunny.LibSql.Client;

public class LibSqlException(string query, string message) : Exception($"{message} (Query: {query})")
{
    public string Query { get; } = query;
    public override string ToString() => $"{Message} (Query: {Query})";
}