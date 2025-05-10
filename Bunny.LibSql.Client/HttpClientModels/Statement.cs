using System.Text.Json.Serialization;

namespace Bunny.LibSql.Client.HttpClientModels;

public class Statement
{
    [JsonPropertyName("sql")]
    public string Sql { get; set; }

    [JsonPropertyName("args")] public List<LibSqlValue> Args { get; set; } = [];
}