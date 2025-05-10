using System.Text.Json.Serialization;
using Bunny.LibSql.Client.Json;

namespace Bunny.LibSql.Client.HttpClientModels;

public class PipelineCall
{
    [JsonPropertyName("requests")] 
    public List<PipelineRequest> Requests { get; set; } = [];
}