using System.Text.Json.Serialization;

namespace Bunny.LibSql.Client.Json;

public class PipelineCall
{
    [JsonPropertyName("requests")] 
    public List<PipelineRequest> Requests { get; set; } = [];
}