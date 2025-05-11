using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Bunny.LibSql.Client.Json.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QueryDeclType
{
    [JsonStringEnumMemberName("INTEGER")]
    Integer,
    [JsonStringEnumMemberName("INT")]
    Int,
    [JsonStringEnumMemberName("REAL")]
    Real,
    [JsonStringEnumMemberName("TEXT")]
    Text,
    [JsonStringEnumMemberName("BLOB")]
    Blob,
    [JsonStringEnumMemberName("boolean")]
    Boolean,
    [JsonStringEnumMemberName("bigint")]
    Bigint,
}