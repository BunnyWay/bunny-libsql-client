using System.ComponentModel.DataAnnotations;
using Bunny.LibSql.Client.Attributes;

namespace Bunny.LibSql.Client.Demo;

public class Description
{
    [Key]
    public int id { get; set; }
    public string name { get; set; }
    
    [ForeignKeyFor(typeof(Product))]
    public string product_id { get; set; }
}