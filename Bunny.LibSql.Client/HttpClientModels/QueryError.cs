using System.Text.Json.Serialization;

namespace Bunny.LibSql.Client.HttpClientModels;

public class QueryError
{
    [JsonPropertyName("message")]
    public string Message { get; set; }
    
    [JsonPropertyName("code")]
    public string Code { get; set; }
}