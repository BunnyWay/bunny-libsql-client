using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Bunny.LibSql.Client.Json.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QueryDeclType
{
    [JsonStringEnumMemberName("INT")]
    Int,
    [JsonStringEnumMemberName("INTEGER")]
    Integer,
    [JsonStringEnumMemberName("REAL")]
    Real,
    [JsonStringEnumMemberName("TEXT")]
    Text,
    [JsonStringEnumMemberName("BLOB")]
    Blob,
}