namespace Bunny.LibSql.Client;

public class LibSqlClientException : Exception
{
    public string? PostJson { get; }
    public string? ResponseJson { get; }
    public string? Query { get; }

    public LibSqlClientException(string message, string? postJson, string? responseJson, string? query, Exception? innerException) : base(message, innerException)
    {
        PostJson = postJson;
        ResponseJson = responseJson;
        Query = query;
    }
}