using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bunny.LibSql.Client.Attributes;
using Bunny.LibSql.Client.SQL;
using Bunny.LibSql.Client.Types;

namespace Bunny.LibSql.Client.Tests;

public class SqlQueryBuilderTests
{
    [Table("items")]
    private sealed class BoolModel
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; }
    }

    [Table("items")]
    private sealed class NullableBoolModel
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        public bool? Hidden { get; set; }
    }

    [Table("vectors")]
    private sealed class F32BlobModel
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [BlobSize(3)]
        public F32Blob Embedding { get; set; } = null!;
    }

    // -- Bool in UPDATE --

    [Test]
    public void BuildUpdateQuery_BoolTrue_IncludedInSetClause()
    {
        var item = new BoolModel { Id = "1", Name = "test", Active = true };
        var query = SqlQueryBuilder.BuildUpdateQuery("items", item, "Id", "1");

        Assert.That(query.sql, Does.Contain("Active = ?"));
        Assert.That(query.args, Has.Some.TypeOf<bool>());
        Assert.That(query.args!.OfType<bool>().First(), Is.True);
    }

    [Test]
    public void BuildUpdateQuery_BoolFalse_IncludedInSetClause()
    {
        var item = new BoolModel { Id = "1", Name = "test", Active = false };
        var query = SqlQueryBuilder.BuildUpdateQuery("items", item, "Id", "1");

        // false is not null, so it should be included
        Assert.That(query.sql, Does.Contain("Active = ?"));
        Assert.That(query.args!.OfType<bool>().First(), Is.False);
    }

    // -- Nullable Bool in UPDATE --

    [Test]
    public void BuildUpdateQuery_NullableBoolTrue_IncludedInSetClause()
    {
        var item = new NullableBoolModel { Id = "1", Hidden = true };
        var query = SqlQueryBuilder.BuildUpdateQuery("items", item, "Id", "1");

        Assert.That(query.sql, Does.Contain("Hidden = ?"));
        Assert.That(query.args!.OfType<bool>().First(), Is.True);
    }

    [Test]
    public void BuildUpdateQuery_NullableBoolNull_ExcludedFromSetClause()
    {
        var item = new NullableBoolModel { Id = "1", Hidden = null };
        var query = SqlQueryBuilder.BuildUpdateQuery("items", item, "Id", "1");

        Assert.That(query.sql, Does.Not.Contain("Hidden = ?"));
    }

    // -- Bool in INSERT --

    [Test]
    public void BuildInsertQuery_BoolTrue_IncludedAsParameter()
    {
        var item = new BoolModel { Id = "1", Name = "test", Active = true };
        var query = SqlQueryBuilder.BuildInsertQuery("items", item);

        Assert.That(query.sql, Does.Contain("Active"));
        Assert.That(query.args!.OfType<bool>().First(), Is.True);
    }

    [Test]
    public void BuildInsertQuery_BoolFalse_IncludedAsParameter()
    {
        var item = new BoolModel { Id = "1", Name = "test", Active = false };
        var query = SqlQueryBuilder.BuildInsertQuery("items", item);

        Assert.That(query.sql, Does.Contain("Active"));
        Assert.That(query.args!.OfType<bool>().First(), Is.False);
    }

    // -- F32Blob in UPDATE --

    [Test]
    public void BuildUpdateQuery_F32Blob_ConvertedToByteArray()
    {
        var floats = new float[] { 1.0f, 2.0f, 3.0f };
        var item = new F32BlobModel { Id = "1", Embedding = new F32Blob(floats) };
        var query = SqlQueryBuilder.BuildUpdateQuery("vectors", item, "Id", "1");

        Assert.That(query.sql, Does.Contain("Embedding = ?"));
        // GetLibSqlJsonValue() converts F32Blob → byte[]
        Assert.That(query.args!.OfType<byte[]>().Any(), Is.True);
    }

    [Test]
    public void BuildUpdateQuery_F32Blob_ByteArrayMatchesOriginal()
    {
        var floats = new float[] { 1.0f, 2.0f, 3.0f };
        var item = new F32BlobModel { Id = "1", Embedding = new F32Blob(floats) };
        var query = SqlQueryBuilder.BuildUpdateQuery("vectors", item, "Id", "1");

        var bytes = query.args!.OfType<byte[]>().First();
        var roundTripped = new F32Blob(bytes);
        Assert.That(roundTripped.values, Is.EqualTo(floats));
    }
}
