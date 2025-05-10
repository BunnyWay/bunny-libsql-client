using System.Text.Json.Serialization;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.Json;

public class PipelineRequest
{
    [JsonPropertyName("type")]
    public PipelineRequestType Type { get; set; }
    
    [JsonPropertyName("stmt")]
    public Statement Stmt { get; set; }
}