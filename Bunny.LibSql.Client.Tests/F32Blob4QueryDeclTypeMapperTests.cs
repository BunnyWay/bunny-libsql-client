using System.Reflection;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.TypeHandling;
using Bunny.LibSql.Client.Types;

namespace Bunny.LibSql.Client.Tests;

public class F32Blob4QueryDeclTypeMapperTests
{
    private sealed class TestModel
    {
        public F32Blob? Vector { get; set; }
    }

    private sealed class InvalidModel
    {
        public string? Vector { get; set; }
    }

    [Test]
    public void MapF32Blob_Base64Bytes_ToF32Blob()
    {
        var bytes = GetBytes(1f, 2f, 3f, 4f);
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Base64 = bytes
        };

        Assign(QueryDeclType.F32Blob4, nameof(TestModel.Vector), model, libSqlValue);

        Assert.That(model.Vector, Is.Not.Null);
        Assert.That(model.Vector!.values, Is.EqualTo(new[] { 1f, 2f, 3f, 4f }));
    }

    [Test]
    public void MapF32Blob_StringValue_ToF32Blob()
    {
        var bytes = GetBytes(1f, 2f, 3f, 4f);
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = Convert.ToBase64String(bytes)
        };

        Assign(QueryDeclType.F32Blob4, nameof(TestModel.Vector), model, libSqlValue);

        Assert.That(model.Vector, Is.Not.Null);
        Assert.That(model.Vector!.values, Is.EqualTo(new[] { 1f, 2f, 3f, 4f }));
    }

    [Test]
    public void MapF32Blob_JsonString_ToF32Blob()
    {
        var bytes = GetBytes(1f, 2f, 3f, 4f);
        using var doc = JsonDocument.Parse($"\"{Convert.ToBase64String(bytes)}\"");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.F32Blob4, nameof(TestModel.Vector), model, libSqlValue);

        Assert.That(model.Vector, Is.Not.Null);
        Assert.That(model.Vector!.values, Is.EqualTo(new[] { 1f, 2f, 3f, 4f }));
    }

    [Test]
    public void MapF32Blob_JsonNumber_DoesNotChange()
    {
        using var doc = JsonDocument.Parse("123");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.F32Blob4, nameof(TestModel.Vector), model, libSqlValue);

        Assert.That(model.Vector, Is.Null);
    }

    [Test]
    public void MapF32Blob_InvalidBase64String_DoesNotChange()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = "not-base64"
        };

        Assign(QueryDeclType.F32Blob4, nameof(TestModel.Vector), model, libSqlValue);

        Assert.That(model.Vector, Is.Null);
    }

    [Test]
    public void MapF32Blob_Null_ToNull()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Null,
            Value = null
        };

        Assign(QueryDeclType.F32Blob4, nameof(TestModel.Vector), model, libSqlValue);

        Assert.That(model.Vector, Is.Null);
    }

    [Test]
    public void MapF32Blob_NonF32Property_DoesNotChange()
    {
        var model = new InvalidModel
        {
            Vector = "keep"
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = "AQIDBA=="
        };

        Assign(QueryDeclType.F32Blob4, nameof(InvalidModel.Vector), model, libSqlValue);

        Assert.That(model.Vector, Is.EqualTo("keep"));
    }

    [Test]
    public void MapF32Blob_JsonNull_ToNull()
    {
        using var doc = JsonDocument.Parse("null");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.F32Blob4, nameof(TestModel.Vector), model, libSqlValue);

        Assert.That(model.Vector, Is.Null);
    }

    [Test]
    public void MapF32Blob_InvalidSize_DoesNotChange()
    {
        var bytes = GetBytes(1f, 2f);
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Blob,
            Base64 = bytes
        };

        Assign(QueryDeclType.F32Blob4, nameof(TestModel.Vector), model, libSqlValue);

        Assert.That(model.Vector, Is.Null);
    }

    private static void Assign<TModel>(QueryDeclType declType, string propertyName, TModel model, LibSqlValue value)
    {
        var property = typeof(TModel).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null);

        LibSqlToNativeValueMapper.AssignLibSqlValueToNativeProperty(declType, property!, model, value);
    }

    private static byte[] GetBytes(params float[] values)
    {
        var bytes = new byte[values.Length * sizeof(float)];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
