using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Bunny.LibSql.Client.Json.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LibSqlValueType
{
    [JsonStringEnumMemberName("text")]
    Text,
    [JsonStringEnumMemberName("float")]
    Float,
    [JsonStringEnumMemberName("integer")]
    Integer,
    [JsonStringEnumMemberName("null")]
    Null,
}