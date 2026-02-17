using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bunny.LibSql.Client.LINQ;
using Bunny.LibSql.Client.SQL;

namespace Bunny.LibSql.Client.Tests;

public class LinqToSqliteVisitorTests
{
    [Table("entity")]
    private class Entity
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    [Test]
    public void ListContains_Generates_InClause()
    {
        var data = new List<Entity>().AsQueryable();
        var categoryIds = new List<string> { "id1", "id2", "id3" };
        var query = data.Where(tc => categoryIds.Contains(tc.Id));

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT entity.* FROM entity WHERE Id IN (?, ?, ?);"));
            Assert.That(Parameters.ToArray(), Is.EqualTo(new object[] { "id1", "id2", "id3" }));
        });
    }

    [Test]
    public void ListContains_SingleElement_Generates_InClause()
    {
        var data = new List<Entity>().AsQueryable();
        var categoryIds = new List<string> { "only" };
        var query = data.Where(tc => categoryIds.Contains(tc.Id));

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT entity.* FROM entity WHERE Id IN (?);"));
            Assert.That(Parameters.ToArray(), Is.EqualTo(new object[] { "only" }));
        });
    }

    [Test]
    public void ListContains_WithOtherCondition_Generates_InClauseWithAnd()
    {
        var data = new List<Entity>().AsQueryable();
        var categoryIds = new List<string> { "id1", "id2" };
        var query = data.Where(tc => categoryIds.Contains(tc.Id) && tc.Name == "test");

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT entity.* FROM entity WHERE (Id IN (?, ?) AND (Name = ?));"));
            Assert.That(Parameters.ToArray(), Is.EqualTo(new object[] { "id1", "id2", "test" }));
        });
    }
}
