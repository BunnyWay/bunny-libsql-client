using System.Reflection;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.TypeHandling;

namespace Bunny.LibSql.Client.Tests;

public class TextQueryDeclTypeMapperTests
{
    private sealed class TestModel
    {
        public string? TextValue { get; set; }
    }

    private sealed class InvalidModel
    {
        public int TextValue { get; set; }
    }

    [Test]
    public void MapText_JsonString_ToString()
    {
        using var doc = JsonDocument.Parse("\"hello\"");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Text,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Text, nameof(TestModel.TextValue), model, libSqlValue);

        Assert.That(model.TextValue, Is.EqualTo("hello"));
    }

    [Test]
    public void MapText_NullValue_ToNull()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Null,
            Value = null
        };

        Assign(QueryDeclType.Text, nameof(TestModel.TextValue), model, libSqlValue);

        Assert.That(model.TextValue, Is.Null);
    }

    [Test]
    public void MapText_JsonNull_ToNull()
    {
        using var doc = JsonDocument.Parse("null");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Text,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Text, nameof(TestModel.TextValue), model, libSqlValue);

        Assert.That(model.TextValue, Is.Null);
    }

    [Test]
    public void MapText_RawString_ToString()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Text,
            Value = "hello"
        };

        Assign(QueryDeclType.Text, nameof(TestModel.TextValue), model, libSqlValue);

        Assert.That(model.TextValue, Is.EqualTo("hello"));
    }

    [Test]
    public void MapText_Number_ToString()
    {
        using var doc = JsonDocument.Parse("123");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Text,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Text, nameof(TestModel.TextValue), model, libSqlValue);

        Assert.That(model.TextValue, Is.EqualTo("123"));
    }

    [Test]
    public void MapText_JsonBoolean_ToString()
    {
        using var doc = JsonDocument.Parse("true");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Text,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Text, nameof(TestModel.TextValue), model, libSqlValue);

        Assert.That(model.TextValue, Is.EqualTo("true"));
    }

    [Test]
    public void MapText_JsonArray_ToString()
    {
        using var doc = JsonDocument.Parse("[1,2]");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Text,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Text, nameof(TestModel.TextValue), model, libSqlValue);

        Assert.That(model.TextValue, Is.EqualTo("[1,2]"));
    }

    [Test]
    public void MapText_JsonObject_ToString()
    {
        using var doc = JsonDocument.Parse("{\"a\":1}");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Text,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Text, nameof(TestModel.TextValue), model, libSqlValue);

        Assert.That(model.TextValue, Is.EqualTo("{\"a\":1}"));
    }

    [Test]
    public void MapText_Object_ToString()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Text,
            Value = 456
        };

        Assign(QueryDeclType.Text, nameof(TestModel.TextValue), model, libSqlValue);

        Assert.That(model.TextValue, Is.EqualTo("456"));
    }

    [Test]
    public void MapText_NonStringProperty_DoesNotChange()
    {
        var model = new InvalidModel
        {
            TextValue = 7
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Text,
            Value = "hello"
        };

        Assign(QueryDeclType.Text, nameof(InvalidModel.TextValue), model, libSqlValue);

        Assert.That(model.TextValue, Is.EqualTo(7));
    }

    private static void Assign<TModel>(QueryDeclType declType, string propertyName, TModel model, LibSqlValue value)
    {
        var property = typeof(TModel).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null);

        LibSqlToNativeValueMapper.AssignLibSqlValueToNativeProperty(declType, property!, model, value);
    }
}
