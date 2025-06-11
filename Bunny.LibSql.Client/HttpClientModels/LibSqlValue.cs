using System.Text.Json.Serialization;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.TypeHandling;

namespace Bunny.LibSql.Client.HttpClientModels;

public class LibSqlValue
{
    [JsonPropertyName("type")]
    public LibSqlValueType Type { get; set; }
    
    [JsonPropertyName("value")]
    public object? Value { get; set; }
    
    [JsonConverter(typeof(Base64UrlToByteArrayConverter))]
    [JsonPropertyName("base64")]
    public byte[]? Base64 { get; set; }
}