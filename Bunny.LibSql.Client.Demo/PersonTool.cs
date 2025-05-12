using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bunny.LibSql.Client.Attributes;

namespace Bunny.LibSql.Client.Demo;

public class PersonTool
{
    [Key]
    public long id { get; set; }
    
    [ForeignKeyFor(typeof(Person))]
    public long person_id { get; set; }
    [ForeignKeyFor(typeof(Tool))]
    public long tool_id { get; set; }
}