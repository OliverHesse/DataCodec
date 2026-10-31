using DataCodec.Api.Serialization;

namespace DataCodec.Tests.Api.Serialization;

public class SerializationArrayTests
{
    [Fact]
    public void NewArray_IsEmpty()
    {
        var array = new SerializationArray();

        Assert.Empty(array);
        Assert.Equal(0, array.Count);
    }

    [Fact]
    public void AppendObjectAndArray_ReturnNewChildren()
    {
        var array = new SerializationObject().WriteArray("items");

        var obj = array.AppendObject();
        var nested = array.AppendArray();

        Assert.Same(obj, array[0]);
        Assert.Same(nested, array[1]);
    }

    [Fact]
    public void AppendPrimitives_StoreKindAndValue()
    {
        var array = new SerializationArray();

        array.AppendInt(1);
        array.AppendLong(2L);
        array.AppendFloat(1.5f);
        array.AppendDouble(2.5);
        array.AppendDecimal(0.1m);
        array.AppendString("hi");
        array.AppendBool(false);
        array.AppendNull();

        var primitives = array.Cast<SerializationPrimitive>().ToArray();
        Assert.Equal(
            new[] { PrimitiveKind.Int, PrimitiveKind.Long, PrimitiveKind.Float, PrimitiveKind.Double,
                PrimitiveKind.Decimal, PrimitiveKind.String, PrimitiveKind.Bool, PrimitiveKind.Null },
            primitives.Select(p => p.Kind));
        Assert.Equal(new object?[] { 1, 2L, 1.5f, 2.5, 0.1m, "hi", false, null }, primitives.Select(p => p.Value));
    }

    [Fact]
    public void Items_CanBeMixed()
    {
        var array = new SerializationArray();

        array.AppendInt(1);
        array.AppendObject().WriteString("name", "sword");
        array.AppendArray().AppendInt(2);

        Assert.Equal(3, array.Count);
        Assert.IsType<SerializationPrimitive>(array[0]);
        Assert.IsType<SerializationObject>(array[1]);
        Assert.IsType<SerializationArray>(array[2]);
    }

    [Fact]
    public void AppendString_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SerializationArray().AppendString(null!));
    }

    [Fact]
    public void RemoveAt_RemovesItem()
    {
        var array = new SerializationArray();
        array.AppendObject();
        var second = array.AppendObject();

        array.RemoveAt(0);

        Assert.Single(array);
        Assert.Same(second, array[0]);
    }
}
