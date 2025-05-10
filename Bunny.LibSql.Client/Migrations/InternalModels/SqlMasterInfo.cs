namespace Bunny.LibSql.Client.Migrations.InternalModels;

public class SqlMasterInfo
{
    public string type { get; set; }
    public string name { get; set; }
    public string tbl_name { get; set; }
    public int rootpage { get; set; }
    public string sql { get; set; }
}