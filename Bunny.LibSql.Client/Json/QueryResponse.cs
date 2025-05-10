using System.Text.Json.Serialization;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.Json;

public class QueryResponse
{
    [JsonPropertyName("type")]
    public QueryResponseType Type { get; set; }
    
    [JsonPropertyName("result")]
    public QueryResult Result { get; set; }
}