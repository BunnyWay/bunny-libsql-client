using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Bunny.LibSql.Client.Json;

namespace Bunny.LibSql.Client
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