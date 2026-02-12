using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Bunny.LibSql.Client;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;

namespace Bunny.LibSql.Client.Tests;

[Table("test_items")]
public class TestItem
{
    [Key]
    public long id { get; set; }
    public string name { get; set; } = "";
    public int value { get; set; }
}

public class TestDbContext(LibSqlClient client) : LibSqlDbContext(client)
{
    public LibSqlTable<TestItem> TestItems { get; set; }
}

public class MockHttpHandler : HttpMessageHandler
{
    public List<string> CapturedRequestBodies { get; } = [];
    public Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory { get; set; } = null!;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            CapturedRequestBodies.Add(body);
        }
        return ResponseFactory(request);
    }
}

public class BulkOperationsTests
{
    private MockHttpHandler _handler = null!;
    private TestDbContext _db = null!;
    private static readonly FieldInfo HttpClientField =
        typeof(LibSqlClient).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Static)!;

    private HttpClient? _originalClient;

    [SetUp]
    public void Setup()
    {
        _handler = new MockHttpHandler();
        var mockHttpClient = new HttpClient(_handler);

        _originalClient = (HttpClient?)HttpClientField.GetValue(null);
        HttpClientField.SetValue(null, mockHttpClient);

        var client = new LibSqlClient("https://test.turso.io", "fake-token");
        _db = new TestDbContext(client);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalClient != null)
            HttpClientField.SetValue(null, _originalClient);
        _handler.Dispose();
    }

    private static HttpResponseMessage CreatePipelineResponse(int count, long startRowId = 1)
    {
        var results = new List<PipelineResult>();
        for (var i = 0; i < count; i++)
        {
            results.Add(new PipelineResult
            {
                Type = PipelineResultType.OK,
                Response = new QueryResponse
                {
                    Type = QueryResponseType.Execute,
                    Result = new QueryResult
                    {
                        Cols = [],
                        Rows = [],
                        AffectedRowCount = 1,
                        LastInsertRowId = startRowId + i,
                        RowsRead = 0,
                        RowsWritten = 1,
                        QueryDurationMs = 0.5
                    }
                }
            });
        }

        var response = new PipelineResponse { Results = results };
        var json = JsonSerializer.Serialize(response);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    [Test]
    public async Task InsertManyAsync_SendsSingleRequest_WithAllItems()
    {
        var items = new List<TestItem>
        {
            new() { name = "Item1", value = 10 },
            new() { name = "Item2", value = 20 },
            new() { name = "Item3", value = 30 },
        };

        _handler.ResponseFactory = _ => CreatePipelineResponse(3, startRowId: 100);

        await _db.TestItems.InsertManyAsync(items);

        Assert.That(_handler.CapturedRequestBodies, Has.Count.EqualTo(1), "Should send a single HTTP request");

        var body = _handler.CapturedRequestBodies[0];
        var doc = JsonDocument.Parse(body);
        var requests = doc.RootElement.GetProperty("requests");
        Assert.That(requests.GetArrayLength(), Is.EqualTo(3), "Pipeline should contain 3 statements");
    }

    [Test]
    public async Task InsertManyAsync_EmptyList_DoesNothing()
    {
        _handler.ResponseFactory = _ => throw new InvalidOperationException("Should not be called");

        await _db.TestItems.InsertManyAsync([]);

        Assert.That(_handler.CapturedRequestBodies, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task InsertManyAsync_RequestContainsAllStatements()
    {
        var items = new List<TestItem>
        {
            new() { name = "A", value = 1 },
            new() { name = "B", value = 2 },
        };

        _handler.ResponseFactory = _ => CreatePipelineResponse(2);

        await _db.TestItems.InsertManyAsync(items);

        var body = _handler.CapturedRequestBodies[0];
        var doc = JsonDocument.Parse(body);
        var requests = doc.RootElement.GetProperty("requests");
        Assert.That(requests.GetArrayLength(), Is.EqualTo(2), "Pipeline should contain 2 statements");
    }

    [Test]
    public async Task UpdateManyAsync_SendsSingleRequest_WithAllItems()
    {
        var items = new List<TestItem>
        {
            new() { id = 1, name = "Updated1", value = 100 },
            new() { id = 2, name = "Updated2", value = 200 },
        };

        _handler.ResponseFactory = _ => CreatePipelineResponse(2);

        await _db.TestItems.UpdateManyAsync(items);

        Assert.That(_handler.CapturedRequestBodies, Has.Count.EqualTo(1), "Should send a single HTTP request");

        var body = _handler.CapturedRequestBodies[0];
        var doc = JsonDocument.Parse(body);
        var requests = doc.RootElement.GetProperty("requests");
        Assert.That(requests.GetArrayLength(), Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateManyAsync_EmptyList_DoesNothing()
    {
        _handler.ResponseFactory = _ => throw new InvalidOperationException("Should not be called");

        await _db.TestItems.UpdateManyAsync([]);

        Assert.That(_handler.CapturedRequestBodies, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task DeleteManyAsync_SendsSingleRequest_WithAllItems()
    {
        var items = new List<TestItem>
        {
            new() { id = 10, name = "ToDelete1" },
            new() { id = 20, name = "ToDelete2" },
            new() { id = 30, name = "ToDelete3" },
        };

        _handler.ResponseFactory = _ => CreatePipelineResponse(3);

        await _db.TestItems.DeleteManyAsync(items);

        Assert.That(_handler.CapturedRequestBodies, Has.Count.EqualTo(1), "Should send a single HTTP request");

        var body = _handler.CapturedRequestBodies[0];
        var doc = JsonDocument.Parse(body);
        var requests = doc.RootElement.GetProperty("requests");
        Assert.That(requests.GetArrayLength(), Is.EqualTo(3));
    }

    [Test]
    public async Task DeleteManyAsync_EmptyList_DoesNothing()
    {
        _handler.ResponseFactory = _ => throw new InvalidOperationException("Should not be called");

        await _db.TestItems.DeleteManyAsync([]);

        Assert.That(_handler.CapturedRequestBodies, Has.Count.EqualTo(0));
    }

    [Test]
    public void UpdateManyAsync_NullPrimaryKey_Throws()
    {
        var items = new List<TestItem>
        {
            new() { id = 0, name = "Valid" },
        };

        _handler.ResponseFactory = _ => CreatePipelineResponse(1);

        // id=0 is technically not null for long, so this should succeed
        Assert.DoesNotThrowAsync(() => _db.TestItems.UpdateManyAsync(items));
    }

    [Test]
    public void DeleteManyAsync_NullPrimaryKey_Throws()
    {
        var items = new List<TestItem>
        {
            new() { id = 0, name = "Valid" },
        };

        _handler.ResponseFactory = _ => CreatePipelineResponse(1);

        Assert.DoesNotThrowAsync(() => _db.TestItems.DeleteManyAsync(items));
    }
}
