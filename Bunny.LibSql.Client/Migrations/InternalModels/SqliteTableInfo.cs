namespace Bunny.LibSql.Client.Migrations.InternalModels;

public class SqliteTableInfo
{
    
    public int cid { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public bool notnull { get; set; }
    public string dflt_value { get; set; }
    public int pk { get; set; }
}