using System.ComponentModel.DataAnnotations;

namespace Bunny.LibSql.Client.Tests.Models;

public class StringKey
{
    [Key]
    public string id { get; set; }
    public string description { get; set; }
}