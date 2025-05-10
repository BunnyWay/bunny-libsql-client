using System.Text.Json.Serialization;
using Bunny.LibSql.Client.Json;

namespace Bunny.LibSql.Client.HttpClientModels
{
    public class PipelineResponse
    {
        [JsonPropertyName("baton")]
        public string Baton { get; set; }

        [JsonPropertyName("base_url")]
        public string BaseUrl { get; set; }

        [JsonPropertyName("results")]
        public List<PipelineResult> Results { get; set; }
    }
}