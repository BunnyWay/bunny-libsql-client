using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Bunny.LibSql.Client.Attributes;

namespace Bunny.LibSql.Client.Tests.Models;

public class Product
{
    [Key]
    public int id { get; set; }
    [NotNull]
    [AllowNull]
    [Index]
    public string name { get; set; }
    
    [ForeignKeyFor(typeof(Person))]
    public string person_id { get; set; }
    
    [AutoInclude]
    public Description? descriptions { get; set; }
}