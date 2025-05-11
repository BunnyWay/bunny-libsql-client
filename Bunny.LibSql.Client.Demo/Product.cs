using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Bunny.LibSql.Client.Attributes;
using Bunny.LibSql.Client.Demo;

namespace Bunny.LibSql.Client.Demo;

public class Product
{
    [Key]
    public int id { get; set; }
    [NotNull]
    [AllowNull]
    public string name { get; set; }
    public string person_id { get; set; }
    
    [ForeignKey("product_id")]
    [AutoInclude]
    public Description? descriptions { get; set; }
}