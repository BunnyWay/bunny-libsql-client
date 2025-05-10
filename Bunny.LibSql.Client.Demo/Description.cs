using System.ComponentModel.DataAnnotations;

namespace Bunny.LibSql.Client.Demo;

public class Description
{
    [Key]
    public int id { get; set; }
    public string name { get; set; }
    public string product_id { get; set; }
}