using System.Reflection;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.TypeHandling;

namespace Bunny.LibSql.Client.Tests;

public class RealQueryDeclTypeMapperTests
{
    private sealed class TestModel
    {
        public double DoubleValue { get; set; }
        public float FloatValue { get; set; }
        public double? NullableDoubleValue { get; set; }
        public float? NullableFloatValue { get; set; }
    }

    private sealed class UnsupportedModel
    {
        public decimal DecimalValue { get; set; }
    }

    [Test]
    public void MapReal_JsonNumber_ToDouble()
    {
        using var doc = JsonDocument.Parse("150.5");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Float,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Real, nameof(TestModel.DoubleValue), model, libSqlValue);

        Assert.That(model.DoubleValue, Is.EqualTo(150.5d));
    }

    [Test]
    public void MapReal_JsonString_ToDouble()
    {
        using var doc = JsonDocument.Parse("\"150.5\"");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Text,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Real, nameof(TestModel.DoubleValue), model, libSqlValue);

        Assert.That(model.DoubleValue, Is.EqualTo(150.5d));
    }

    [Test]
    public void MapReal_IntegerValue_ToDouble()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = 150L
        };

        Assign(QueryDeclType.Real, nameof(TestModel.DoubleValue), model, libSqlValue);

        Assert.That(model.DoubleValue, Is.EqualTo(150d));
    }

    [Test]
    public void MapReal_JsonNumber_ToFloat()
    {
        using var doc = JsonDocument.Parse("150.5");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Float,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Real, nameof(TestModel.FloatValue), model, libSqlValue);

        Assert.That(model.FloatValue, Is.EqualTo(150.5f));
    }

    [Test]
    public void MapReal_JsonNumber_ToNullableDouble()
    {
        using var doc = JsonDocument.Parse("150.5");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Float,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Real, nameof(TestModel.NullableDoubleValue), model, libSqlValue);

        Assert.That(model.NullableDoubleValue, Is.EqualTo(150.5d));
    }

    [Test]
    public void MapReal_JsonNumber_ToNullableFloat()
    {
        using var doc = JsonDocument.Parse("150.5");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Float,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Real, nameof(TestModel.NullableFloatValue), model, libSqlValue);

        Assert.That(model.NullableFloatValue, Is.EqualTo(150.5f));
    }

    [Test]
    public void MapReal_Null_ToNullableFloat()
    {
        var model = new TestModel
        {
            NullableFloatValue = 1.5f
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Null,
            Value = null
        };

        Assign(QueryDeclType.Real, nameof(TestModel.NullableFloatValue), model, libSqlValue);

        Assert.That(model.NullableFloatValue, Is.Null);
    }

    [Test]
    public void MapReal_Null_ToNullableDouble()
    {
        var model = new TestModel
        {
            NullableDoubleValue = 12.5
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Null,
            Value = null
        };

        Assign(QueryDeclType.Real, nameof(TestModel.NullableDoubleValue), model, libSqlValue);

        Assert.That(model.NullableDoubleValue, Is.Null);
    }

    [Test]
    public void MapReal_Null_ToNonNullable_DoesNotChange()
    {
        var model = new TestModel
        {
            DoubleValue = 12.5
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Null,
            Value = null
        };

        Assign(QueryDeclType.Real, nameof(TestModel.DoubleValue), model, libSqlValue);

        Assert.That(model.DoubleValue, Is.EqualTo(12.5d));
    }

    [Test]
    public void MapReal_InvalidString_DoesNotChange()
    {
        var model = new TestModel
        {
            DoubleValue = 7.5
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Text,
            Value = "not-a-number"
        };

        Assign(QueryDeclType.Real, nameof(TestModel.DoubleValue), model, libSqlValue);

        Assert.That(model.DoubleValue, Is.EqualTo(7.5d));
    }

    [Test]
    public void MapReal_UnsupportedProperty_DoesNotChange()
    {
        var model = new UnsupportedModel
        {
            DecimalValue = 1.5m
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Float,
            Value = 1.23d
        };

        Assign(QueryDeclType.Real, nameof(UnsupportedModel.DecimalValue), model, libSqlValue);

        Assert.That(model.DecimalValue, Is.EqualTo(1.5m));
    }

    [Test]
    public void MapReal_FloatOverflow_DoesNotChange()
    {
        var model = new TestModel
        {
            FloatValue = 2.5f
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Float,
            Value = 1e39d
        };

        Assign(QueryDeclType.Real, nameof(TestModel.FloatValue), model, libSqlValue);

        Assert.That(model.FloatValue, Is.EqualTo(2.5f));
    }

    [Test]
    public void MapReal_FloatUnderflow_DoesNotChange()
    {
        var model = new TestModel
        {
            FloatValue = 2.5f
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Float,
            Value = 1e-50d
        };

        Assign(QueryDeclType.Real, nameof(TestModel.FloatValue), model, libSqlValue);

        Assert.That(model.FloatValue, Is.EqualTo(2.5f));
    }

    private static void Assign<TModel>(QueryDeclType declType, string propertyName, TModel model, LibSqlValue value)
    {
        var property = typeof(TModel).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null);

        LibSqlToNativeValueMapper.AssignLibSqlValueToNativeProperty(declType, property!, model, value);
    }
}
