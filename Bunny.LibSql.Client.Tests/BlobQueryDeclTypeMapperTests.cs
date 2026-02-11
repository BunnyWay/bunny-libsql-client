using System.Reflection;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.TypeHandling;

namespace Bunny.LibSql.Client.Tests;

public class BlobQueryDeclTypeMapperTests
{
    private sealed class TestModel
    {
        public byte[]? BytesValue { get; set; }
        public string? Base64Value { get; set; }
        public int NonBlobValue { get; set; }
    }

    [Test]
    public void MapBlob_Base64Bytes_ToByteArray()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Base64 = bytes
        };

        Assign(QueryDeclType.Blob, nameof(TestModel.BytesValue), model, libSqlValue);

        Assert.That(model.BytesValue, Is.EqualTo(bytes));
    }

    [Test]
    public void MapBlob_NullValue_ToNullByteArray()
    {
        var model = new TestModel
        {
            BytesValue = new byte[] { 9 }
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Null,
            Value = null
        };

        Assign(QueryDeclType.Blob, nameof(TestModel.BytesValue), model, libSqlValue);

        Assert.That(model.BytesValue, Is.Null);
    }

    [Test]
    public void MapBlob_StringValue_ToByteArray()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = "AQID"
        };

        Assign(QueryDeclType.Blob, nameof(TestModel.BytesValue), model, libSqlValue);

        Assert.That(model.BytesValue, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void MapBlob_JsonString_ToByteArray()
    {
        using var doc = JsonDocument.Parse("\"AQID\"");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Blob, nameof(TestModel.BytesValue), model, libSqlValue);

        Assert.That(model.BytesValue, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void MapBlob_JsonNull_ToNullByteArray()
    {
        using var doc = JsonDocument.Parse("null");
        var model = new TestModel
        {
            BytesValue = [9]
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Blob, nameof(TestModel.BytesValue), model, libSqlValue);

        Assert.That(model.BytesValue, Is.Null);
    }

    [Test]
    public void MapBlob_JsonNull_ToNullString()
    {
        using var doc = JsonDocument.Parse("null");
        var model = new TestModel
        {
            Base64Value = "AQID"
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Blob, nameof(TestModel.Base64Value), model, libSqlValue);

        Assert.That(model.Base64Value, Is.Null);
    }

    [Test]
    public void MapBlob_StringValue_ToString()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = "AQID"
        };

        Assign(QueryDeclType.Blob, nameof(TestModel.Base64Value), model, libSqlValue);

        Assert.That(model.Base64Value, Is.EqualTo("AQID"));
    }

    [Test]
    public void MapBlob_JsonString_ToString()
    {
        using var doc = JsonDocument.Parse("\"AQID\"");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Blob, nameof(TestModel.Base64Value), model, libSqlValue);

        Assert.That(model.Base64Value, Is.EqualTo("AQID"));
    }

    [Test]
    public void MapBlob_Base64Bytes_ToString()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Base64 = bytes
        };

        Assign(QueryDeclType.Blob, nameof(TestModel.Base64Value), model, libSqlValue);

        Assert.That(model.Base64Value, Is.EqualTo("AQID"));
    }

    [Test]
    public void MapBlob_UnsupportedProperty_DoesNotChange()
    {
        var model = new TestModel
        {
            NonBlobValue = 7
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = "AQID"
        };

        Assign(QueryDeclType.Blob, nameof(TestModel.NonBlobValue), model, libSqlValue);

        Assert.That(model.NonBlobValue, Is.EqualTo(7));
    }

    private static void Assign(QueryDeclType declType, string propertyName, TestModel model, LibSqlValue value)
    {
        var property = typeof(TestModel).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null);

        LibSqlToNativeValueMapper.AssignLibSqlValueToNativeProperty(declType, property!, model, value);
    }
}
