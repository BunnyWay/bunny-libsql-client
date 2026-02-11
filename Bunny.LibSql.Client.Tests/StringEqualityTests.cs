using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bunny.LibSql.Client.LINQ;
using Bunny.LibSql.Client.SQL;

namespace Bunny.LibSql.Client.Tests;

public class StringEqualityTests
{
    [Table("products")]
    private class Product
    {
        [Key]
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    [Test]
    public void LinqToSqliteVisitor_Uses_Equality_For_String_Key_With_Variable()
    {
        var data = new List<Product>().AsQueryable();
        var code = "ABC";
        var query = data.Where(x => x.Code == code);

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT products.* FROM products WHERE (Code = ?);"));
            Assert.That(Parameters.ToArray(), Is.EqualTo(new object[] { "ABC" }));
        });
    }

    [Test]
    public void LinqToSqliteVisitor_Translates_StringEquals_To_Binary()
    {
        var data = new List<Product>().AsQueryable();
        var code = "ABC";
        var query = data.Where(x => string.Equals(x.Code, code));

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT products.* FROM products WHERE (Code = ?);"));
            Assert.That(Parameters.ToArray(), Is.EqualTo(new object[] { "ABC" }));
        });
    }
}
