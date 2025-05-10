using System.Text.Json.Serialization;

namespace Bunny.LibSql.Client.Json;

public class QueryResult
{
    [JsonPropertyName("cols")]
    public List<QueryCol> Cols { get; set; }
    
    [JsonPropertyName("rows")]
    public List<List<LibSqlValue>> Rows { get; set; }

    [JsonPropertyName("affected_row_count")]
    public int AffectedRowCount { get; set; }
    
    [JsonPropertyName("last_insert_rowid")]
    public object? LastInsertRowId { get; set; }
    
    [JsonPropertyName("replication_index")]
    public string ReplicationIndex { get; set; }
    
    [JsonPropertyName("rows_read")]
    public int RowsRead { get; set; }
    
    [JsonPropertyName("rows_written")]
    public int RowsWritten { get; set; }
    
    [JsonPropertyName("query_duration_ms")]
    public double QueryDurationMs { get; set; }
    
    [JsonPropertyName("error")]
    public QueryError? Error { get; set; }
}