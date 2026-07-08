using System.ComponentModel.DataAnnotations;
using Bunny.LibSql.Client.Attributes;

namespace Bunny.LibSql.Client.Tests.Models;

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
    
    public int? age_nullable { get; set; }
    
    [ManyToMany(typeof(PersonTool))]
    [AutoInclude]
    public List<Tool> tools { get; set; } = new();
    
    [AutoInclude]
    public List<Product> products { get; set; } = new();
}