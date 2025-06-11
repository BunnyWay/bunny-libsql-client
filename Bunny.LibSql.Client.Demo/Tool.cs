using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bunny.LibSql.Client.Attributes;

namespace Bunny.LibSql.Client.Demo;

public class Tool
{
    [Key]
    public long id { get; set; }
    public string name { get; set; }
}