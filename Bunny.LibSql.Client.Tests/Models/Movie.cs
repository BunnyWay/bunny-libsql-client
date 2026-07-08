using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bunny.LibSql.Client.Attributes;
using Bunny.LibSql.Client.Types;

namespace Bunny.LibSql.Client.Tests.Models;

[Table("movies")]
public class Movie
{
    [Key]
    public long id { get; set; }
    [Index]
    public string title { get; set; }
    public int year { get; set; }
    [BlobSize(4)]
    public F32Blob full_emb { get; set; }
}