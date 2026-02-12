using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Bunny.LibSql.Client;
using Bunny.LibSql.Client.HttpClientModels;

namespace Bunny.LibSql.Client.Tests;

#region Test Models

[Table("test_string_pk")]
public class EntityWithStringPk
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

[Table("test_int_pk")]
public class EntityWithIntPk
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[Table("test_long_pk")]
public class EntityWithLongPk
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion

#region Test DbContexts

public class StringPkContext(LibSqlClient client) : LibSqlDbContext(client)
{
    public LibSqlTable<EntityWithStringPk> Entities { get; set; }
}

public class IntPkContext(LibSqlClient client) : LibSqlDbContext(client)
{
    public LibSqlTable<EntityWithIntPk> Entities { get; set; }
}

public class LongPkContext(LibSqlClient client) : LibSqlDbContext(client)
{
    public LibSqlTable<EntityWithLongPk> Entities { get; set; }
}

#endregion

public class AssignLastInsertRowIdTests
{
    private static readonly LibSqlClient DummyClient = new("https://dummy.turso.io", "fake-token");

    private static PipelineResponse BuildPipelineResponse(object lastInsertRowId)
    {
        return new PipelineResponse
        {
            Results =
            [
                new PipelineResult
                {
                    Response = new QueryResponse
                    {
                        Result = new QueryResult
                        {
                            LastInsertRowId = lastInsertRowId
                        }
                    }
                }
            ]
        };
    }

    private static void InvokeAssignLastInsertRowId<T>(LibSqlTable<T> table, T item, PipelineResponse? response, int itemIndex = 0)
    {
        var method = typeof(LibSqlTable<T>).GetMethod("AssignLastInsertRowId", BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(table, [item, response, itemIndex]);
    }

    #region String PK Tests

    [Test]
    public void StringPk_WithExistingValue_ShouldNotOverwrite()
    {
        var ctx = new StringPkContext(DummyClient);
        var item = new EntityWithStringPk { Id = "my-uuid-123", Name = "Test" };
        var response = BuildPipelineResponse(42L);

        InvokeAssignLastInsertRowId(ctx.Entities, item, response);

        Assert.That(item.Id, Is.EqualTo("my-uuid-123"));
    }

    [Test]
    public void StringPk_WithEmptyValue_ShouldAssignRowId()
    {
        var ctx = new StringPkContext(DummyClient);
        var item = new EntityWithStringPk { Id = "", Name = "Test" };
        var response = BuildPipelineResponse(42L);

        InvokeAssignLastInsertRowId(ctx.Entities, item, response);

        Assert.That(item.Id, Is.EqualTo("42"));
    }

    [Test]
    public void StringPk_WithNullValue_ShouldAssignRowId()
    {
        var ctx = new StringPkContext(DummyClient);
        var item = new EntityWithStringPk { Id = null!, Name = "Test" };
        var response = BuildPipelineResponse(7L);

        InvokeAssignLastInsertRowId(ctx.Entities, item, response);

        Assert.That(item.Id, Is.EqualTo("7"));
    }

    #endregion

    #region Int PK Tests

    [Test]
    public void IntPk_WithDefaultZero_ShouldAssignRowId()
    {
        var ctx = new IntPkContext(DummyClient);
        var item = new EntityWithIntPk { Id = 0, Name = "Test" };
        var response = BuildPipelineResponse(99L);

        InvokeAssignLastInsertRowId(ctx.Entities, item, response);

        Assert.That(item.Id, Is.EqualTo(99));
    }

    [Test]
    public void IntPk_WithExistingValue_ShouldNotOverwrite()
    {
        var ctx = new IntPkContext(DummyClient);
        var item = new EntityWithIntPk { Id = 50, Name = "Test" };
        var response = BuildPipelineResponse(99L);

        InvokeAssignLastInsertRowId(ctx.Entities, item, response);

        Assert.That(item.Id, Is.EqualTo(50));
    }

    #endregion

    #region Long PK Tests

    [Test]
    public void LongPk_WithDefaultZero_ShouldAssignRowId()
    {
        var ctx = new LongPkContext(DummyClient);
        var item = new EntityWithLongPk { Id = 0L, Name = "Test" };
        var response = BuildPipelineResponse(123L);

        InvokeAssignLastInsertRowId(ctx.Entities, item, response);

        Assert.That(item.Id, Is.EqualTo(123L));
    }

    [Test]
    public void LongPk_WithExistingValue_ShouldNotOverwrite()
    {
        var ctx = new LongPkContext(DummyClient);
        var item = new EntityWithLongPk { Id = 10L, Name = "Test" };
        var response = BuildPipelineResponse(123L);

        InvokeAssignLastInsertRowId(ctx.Entities, item, response);

        Assert.That(item.Id, Is.EqualTo(10L));
    }

    #endregion

    #region Null Response Tests

    [Test]
    public void NullResponse_ShouldNotThrow()
    {
        var ctx = new IntPkContext(DummyClient);
        var item = new EntityWithIntPk { Id = 0, Name = "Test" };

        InvokeAssignLastInsertRowId(ctx.Entities, item, null);

        Assert.That(item.Id, Is.EqualTo(0));
    }

    [Test]
    public void NullLastInsertRowId_ShouldNotThrow()
    {
        var ctx = new IntPkContext(DummyClient);
        var item = new EntityWithIntPk { Id = 0, Name = "Test" };
        var response = new PipelineResponse
        {
            Results =
            [
                new PipelineResult
                {
                    Response = new QueryResponse
                    {
                        Result = new QueryResult { LastInsertRowId = null }
                    }
                }
            ]
        };

        InvokeAssignLastInsertRowId(ctx.Entities, item, response);

        Assert.That(item.Id, Is.EqualTo(0));
    }

    #endregion
}
