using System.ComponentModel.DataAnnotations;

namespace Bunny.LibSql.Client.Tests.Models;

public class Tool
{
    [Key]
    public long id { get; set; }
    public string name { get; set; }
}