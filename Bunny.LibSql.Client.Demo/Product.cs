using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bunny.LibSql.Client.Attributes;
using Bunny.LibSql.Client.Demo;

namespace Bunny.LibSql.Client.Demo;

public class Product
{
    [Key]
    public int id { get; set; }
    public string name { get; set; }
    public string person_id { get; set; }
    
    [ForeignKey("product_id")]
    [AutoInclude]
    public List<Description> descriptions { get; set; } = new();
}