using System.Text.Json.Serialization;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.HttpClientModels;

public class LibSqlValue
{
    [JsonPropertyName("type")]
    public LibSqlValueType Type { get; set; }
    
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}