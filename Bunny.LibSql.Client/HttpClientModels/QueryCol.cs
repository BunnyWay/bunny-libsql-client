using System.Text.Json.Serialization;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.HttpClientModels;

public class QueryCol
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("decltype")]
    public QueryDeclType? DeclType { get; set; } = null;
}