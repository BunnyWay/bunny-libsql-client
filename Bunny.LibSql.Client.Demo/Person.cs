using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bunny.LibSql.Client.Attributes;

namespace Bunny.LibSql.Client.Demo;

public class Person
{
    [Key]
    public long id { get; set; }
    [Index]
    public string name { get; set; }
    public string lastName { get; set; }
    public double age { get; set; }
    public string code { get; set; }
    public DateTime date_joined { get; set; }
    
    public int? age2 { get; set; }
    
    [ForeignKey("person_id")]
    [AutoInclude]
    public List<Product> products { get; set; } = new();
}