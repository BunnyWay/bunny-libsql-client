using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bunny.LibSql.Client.Migrations;

namespace Bunny.LibSql.Client.Tests;

public class TableAttributeTests
{
    [Table("people")]
    private class Person
    {
        [Key]
        public int Id { get; set; }
    }

    [Test]
    public void TableSynchronizer_Uses_TableAttribute_For_TableName()
    {
        var sql = TableSynchronizer.GenerateSqlCommands(typeof(Person), [], []);

        Assert.That(sql.Count, Is.EqualTo(1));
        Assert.That(sql[0], Does.Contain("CREATE TABLE IF NOT EXISTS people"));
    }
}
