using System.Reflection;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.TypeHandling;

namespace Bunny.LibSql.Client.Tests;

public class IntegerQueryDeclTypeMapperTests
{
    private sealed class TestModel
    {
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public bool BoolValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }
        public short ShortValue { get; set; }
        public byte ByteValue { get; set; }
        public sbyte SByteValue { get; set; }
        public ushort UShortValue { get; set; }
        public uint UIntValue { get; set; }
        public ulong ULongValue { get; set; }
        public int? NullableIntValue { get; set; }
        public long? NullableLongValue { get; set; }
        public bool? NullableBoolValue { get; set; }
        public DateTime? NullableDateTimeValue { get; set; }
        public DateTimeOffset? NullableDateTimeOffsetValue { get; set; }
    }

    [Test]
    public void MapInteger_JsonNumber_ToInt()
    {
        using var doc = JsonDocument.Parse("123");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.IntValue), model, libSqlValue);

        Assert.That(model.IntValue, Is.EqualTo(123));
    }

    [Test]
    public void MapInteger_Long_ToLong()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = 456L
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.LongValue), model, libSqlValue);

        Assert.That(model.LongValue, Is.EqualTo(456L));
    }

    [Test]
    public void MapInteger_JsonNumber_ToBool()
    {
        using var doc = JsonDocument.Parse("1");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.BoolValue), model, libSqlValue);

        Assert.That(model.BoolValue, Is.True);
    }

    [Test]
    public void MapInteger_Zero_ToBoolFalse()
    {
        using var doc = JsonDocument.Parse("0");
        var model = new TestModel { BoolValue = true };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.BoolValue), model, libSqlValue);

        Assert.That(model.BoolValue, Is.False);
    }

    [Test]
    public void MapInteger_Negative_ToInt()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = -42L
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.IntValue), model, libSqlValue);

        Assert.That(model.IntValue, Is.EqualTo(-42));
    }

    [Test]
    public void MapInteger_JsonNumber_ToDateTime()
    {
        using var doc = JsonDocument.Parse("1700000000");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.DateTimeValue), model, libSqlValue);

        var expected = DateTimeOffset.FromUnixTimeSeconds(1700000000).DateTime;
        Assert.That(model.DateTimeValue, Is.EqualTo(expected));
    }

    [Test]
    public void MapInteger_JsonNumber_ToDateTimeOffset()
    {
        using var doc = JsonDocument.Parse("1700000000");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.DateTimeOffsetValue), model, libSqlValue);

        var expected = DateTimeOffset.FromUnixTimeSeconds(1700000000);
        Assert.That(model.DateTimeOffsetValue, Is.EqualTo(expected));
    }

    [Test]
    public void MapInteger_ToShort()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = 12L
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.ShortValue), model, libSqlValue);

        Assert.That(model.ShortValue, Is.EqualTo((short)12));
    }

    [Test]
    public void MapInteger_ToByte()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = 255L
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.ByteValue), model, libSqlValue);

        Assert.That(model.ByteValue, Is.EqualTo((byte)255));
    }

    [Test]
    public void MapInteger_ToSByte()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = -8L
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.SByteValue), model, libSqlValue);

        Assert.That(model.SByteValue, Is.EqualTo((sbyte)-8));
    }

    [Test]
    public void MapInteger_ToUShort()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = 42L
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.UShortValue), model, libSqlValue);

        Assert.That(model.UShortValue, Is.EqualTo((ushort)42));
    }

    [Test]
    public void MapInteger_ToUInt()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = 42L
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.UIntValue), model, libSqlValue);

        Assert.That(model.UIntValue, Is.EqualTo(42u));
    }

    [Test]
    public void MapInteger_ToULong()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = 42L
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.ULongValue), model, libSqlValue);

        Assert.That(model.ULongValue, Is.EqualTo(42ul));
    }

    [Test]
    public void MapInteger_JsonNumber_ToNullableInt()
    {
        using var doc = JsonDocument.Parse("123");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.NullableIntValue), model, libSqlValue);

        Assert.That(model.NullableIntValue, Is.EqualTo(123));
    }

    [Test]
    public void MapInteger_Long_ToNullableLong()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = 456L
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.NullableLongValue), model, libSqlValue);

        Assert.That(model.NullableLongValue, Is.EqualTo(456L));
    }

    [Test]
    public void MapInteger_JsonNumber_ToNullableBool()
    {
        using var doc = JsonDocument.Parse("1");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.NullableBoolValue), model, libSqlValue);

        Assert.That(model.NullableBoolValue, Is.True);
    }

    [Test]
    public void MapInteger_JsonNumber_ToNullableDateTime()
    {
        using var doc = JsonDocument.Parse("1700000000");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.NullableDateTimeValue), model, libSqlValue);

        var expected = DateTimeOffset.FromUnixTimeSeconds(1700000000).DateTime;
        Assert.That(model.NullableDateTimeValue, Is.EqualTo(expected));
    }

    [Test]
    public void MapInteger_JsonNumber_ToNullableDateTimeOffset()
    {
        using var doc = JsonDocument.Parse("1700000000");
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = doc.RootElement
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.NullableDateTimeOffsetValue), model, libSqlValue);

        var expected = DateTimeOffset.FromUnixTimeSeconds(1700000000);
        Assert.That(model.NullableDateTimeOffsetValue, Is.EqualTo(expected));
    }

    [Test]
    public void MapInteger_Null_ToNullableInt()
    {
        var model = new TestModel();
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Null,
            Value = null
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.NullableIntValue), model, libSqlValue);

        Assert.That(model.NullableIntValue, Is.Null);
    }

    [Test]
    public void MapInteger_Null_ToNonNullable_DoesNotChange()
    {
        var model = new TestModel
        {
            IntValue = 7
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Null,
            Value = null
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.IntValue), model, libSqlValue);

        Assert.That(model.IntValue, Is.EqualTo(7));
    }

    [Test]
    public void MapInteger_InvalidValue_DoesNotChange()
    {
        var model = new TestModel
        {
            IntValue = 7
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = "not-a-number"
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.IntValue), model, libSqlValue);

        Assert.That(model.IntValue, Is.EqualTo(7));
    }

    [Test]
    public void MapInteger_IntOverflow_DoesNotChange()
    {
        var model = new TestModel
        {
            IntValue = 7
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = (long)int.MaxValue + 1
        };

        Assign(QueryDeclType.Integer, nameof(TestModel.IntValue), model, libSqlValue);

        Assert.That(model.IntValue, Is.EqualTo(7));
    }

    [Test]
    public void MapInteger_UnsupportedProperty_DoesNotChange()
    {
        var model = new UnsupportedModel
        {
            DecimalValue = 1.5m
        };
        var libSqlValue = new LibSqlValue
        {
            Type = LibSqlValueType.Integer,
            Value = 1L
        };

        Assign(QueryDeclType.Integer, nameof(UnsupportedModel.DecimalValue), model, libSqlValue);

        Assert.That(model.DecimalValue, Is.EqualTo(1.5m));
    }

    private sealed class UnsupportedModel
    {
        public decimal DecimalValue { get; set; }
    }

    private static void Assign<TModel>(QueryDeclType declType, string propertyName, TModel model, LibSqlValue value)
    {
        var property = typeof(TModel).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null);

        LibSqlToNativeValueMapper.AssignLibSqlValueToNativeProperty(declType, property!, model, value);
    }
}
