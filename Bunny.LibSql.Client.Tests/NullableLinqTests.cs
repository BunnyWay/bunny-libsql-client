using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bunny.LibSql.Client.LINQ;
using Bunny.LibSql.Client.SQL;

namespace Bunny.LibSql.Client.Tests;

public class NullableLinqTests
{
    [Table("items")]
    private class Item
    {
        [Key]
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int? Score { get; set; }

        public double? Rating { get; set; }

        public bool? Active { get; set; }
    }

    [Test]
    public void Where_HasValue_Generates_IsNotNull()
    {
        var data = new List<Item>().AsQueryable();
        var query = data.Where(x => x.Score.HasValue);

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT items.* FROM items WHERE Score IS NOT NULL;"));
            Assert.That(Parameters.ToArray(), Is.Empty);
        });
    }

    [Test]
    public void Where_Not_HasValue_Generates_NotIsNotNull()
    {
        var data = new List<Item>().AsQueryable();
        var query = data.Where(x => !x.Score.HasValue);

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT items.* FROM items WHERE NOT (Score IS NOT NULL);"));
            Assert.That(Parameters.ToArray(), Is.Empty);
        });
    }

    [Test]
    public void Where_Value_In_Comparison_Generates_ColumnName()
    {
        var data = new List<Item>().AsQueryable();
        var query = data.Where(x => x.Score.Value > 10);

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT items.* FROM items WHERE (Score > ?);"));
            Assert.That(Parameters.ToArray(), Is.EqualTo(new object[] { 10 }));
        });
    }

    [Test]
    public void Where_Equals_Null_Generates_IsNull()
    {
        var data = new List<Item>().AsQueryable();
        var query = data.Where(x => x.Score == null);

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT items.* FROM items WHERE (Score IS NULL);"));
            Assert.That(Parameters.ToArray(), Is.Empty);
        });
    }

    [Test]
    public void Where_NotEquals_Null_Generates_IsNotNull()
    {
        var data = new List<Item>().AsQueryable();
        var query = data.Where(x => x.Score != null);

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT items.* FROM items WHERE (Score IS NOT NULL);"));
            Assert.That(Parameters.ToArray(), Is.Empty);
        });
    }

    [Test]
    public void Where_HasValue_Combined_With_Other_Conditions()
    {
        var data = new List<Item>().AsQueryable();
        var query = data.Where(x => x.Score.HasValue && x.Name == "test");

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT items.* FROM items WHERE (Score IS NOT NULL AND (Name = ?));"));
            Assert.That(Parameters.ToArray(), Is.EqualTo(new object[] { "test" }));
        });
    }

    [Test]
    public void Where_Nullable_Equals_Value_Works()
    {
        var data = new List<Item>().AsQueryable();
        var query = data.Where(x => x.Score == 5);

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Does.Contain("Score"));
            Assert.That(Sql, Does.Contain("?"));
        });
    }

    [Test]
    public void Where_Double_Nullable_HasValue()
    {
        var data = new List<Item>().AsQueryable();
        var query = data.Where(x => x.Rating.HasValue);

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT items.* FROM items WHERE Rating IS NOT NULL;"));
            Assert.That(Parameters.ToArray(), Is.Empty);
        });
    }

    [Test]
    public void Where_Bool_Nullable_HasValue()
    {
        var data = new List<Item>().AsQueryable();
        var query = data.Where(x => x.Active.HasValue);

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT items.* FROM items WHERE Active IS NOT NULL;"));
            Assert.That(Parameters.ToArray(), Is.Empty);
        });
    }
}
