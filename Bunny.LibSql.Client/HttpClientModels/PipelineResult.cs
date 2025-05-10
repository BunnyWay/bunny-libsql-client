using System.Text.Json.Serialization;
using Bunny.LibSql.Client.Json;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.HttpClientModels;

public class PipelineResult
{
    [JsonPropertyName("type")]
    public PipelineResultType Type { get; set; }
    
    [JsonPropertyName("response")]
    public QueryResponse? Response { get; set; }
}