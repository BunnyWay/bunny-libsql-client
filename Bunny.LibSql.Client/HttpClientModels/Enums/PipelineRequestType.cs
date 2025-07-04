using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Bunny.LibSql.Client.Json.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PipelineRequestType
{
    [JsonStringEnumMemberName("execute")]
    Execute,
}