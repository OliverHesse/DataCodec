using DataCodec.Api.Serialization;

namespace DataCodec.Tests.Api.Serialization;

public class SerializationObjectTests
{
    private static SerializationPrimitive Primitive(SerializationObject obj, string key)
    {
        Assert.True(obj.TryGet(key, out var element));
        return Assert.IsType<SerializationPrimitive>(element);
    }

    [Fact]
    public void NewObject_IsEmpty()
    {
        var root = new SerializationObject();

        Assert.Equal(0, root.Count);
        Assert.Empty(root.Keys);
    }

    [Fact]
    public void WriteObject_ReturnsChildStoredAtKey()
    {
        var root = new SerializationObject();

        var child = root.WriteObject("stats");

        Assert.True(root.TryGet("stats", out var stored));
        Assert.Same(child, stored);
    }

    [Fact]
    public void WriteArray_ReturnsChildStoredAtKey()
    {
        var root = new SerializationObject();

        var array = root.WriteArray("tag");

        Assert.True(root.TryGet("tag", out var stored));
        Assert.Same(array, stored);
    }

    [Fact]
    public void PrimitiveWriters_StoreKindAndValue()
    {
        var root = new SerializationObject()
            .WriteInt("i", 1)
            .WriteLong("l", 2L)
            .WriteFloat("f", 1.5f)
            .WriteDouble("d", 2.5)
            .WriteDecimal("m", 0.1m)
            .WriteString("s", "hi")
            .WriteBool("b", true)
            .WriteNull("n");

        Assert.Equal((PrimitiveKind.Int, (object?)1), (Primitive(root, "i").Kind, Primitive(root, "i").Value));
        Assert.Equal((PrimitiveKind.Long, (object?)2L), (Primitive(root, "l").Kind, Primitive(root, "l").Value));
        Assert.Equal((PrimitiveKind.Float, (object?)1.5f), (Primitive(root, "f").Kind, Primitive(root, "f").Value));
        Assert.Equal((PrimitiveKind.Double, (object?)2.5), (Primitive(root, "d").Kind, Primitive(root, "d").Value));
        Assert.Equal((PrimitiveKind.Decimal, (object?)0.1m), (Primitive(root, "m").Kind, Primitive(root, "m").Value));
        Assert.Equal((PrimitiveKind.String, (object?)"hi"), (Primitive(root, "s").Kind, Primitive(root, "s").Value));
        Assert.Equal((PrimitiveKind.Bool, (object?)true), (Primitive(root, "b").Kind, Primitive(root, "b").Value));
        Assert.Equal(PrimitiveKind.Null, Primitive(root, "n").Kind);
        Assert.Null(Primitive(root, "n").Value);
    }

    [Fact]
    public void PrimitiveWriters_ReturnSameObject()
    {
        var root = new SerializationObject();

        Assert.Same(root, root.WriteInt("a", 1));
        Assert.Same(root, root.WriteNull("b"));
    }

    [Fact]
    public void DuplicateKey_Throws()
    {
        var root = new SerializationObject().WriteInt("id", 1);

        Assert.Throws<ArgumentException>(() => root.WriteInt("id", 2));
        Assert.Throws<ArgumentException>(() => root.WriteObject("id"));
        Assert.Throws<ArgumentException>(() => root.WriteArray("id"));
    }

    [Fact]
    public void NullKeyOrString_Throws()
    {
        var root = new SerializationObject();

        Assert.Throws<ArgumentNullException>(() => root.WriteInt(null!, 1));
        Assert.Throws<ArgumentNullException>(() => root.WriteObject(null!));
        Assert.Throws<ArgumentNullException>(() => root.WriteString("s", null!));
    }

    [Fact]
    public void Keys_KeepInsertionOrder()
    {
        var root = new SerializationObject().WriteInt("z", 1).WriteInt("a", 2);
        root.WriteObject("m");

        Assert.Equal(new[] { "z", "a", "m" }, root.Keys);
        Assert.Equal(3, root.Count);
        Assert.True(root.ContainsKey("a"));
        Assert.False(root.ContainsKey("b"));
    }

    [Fact]
    public void Remove_FreesKey()
    {
        var root = new SerializationObject();
        root.WriteObject("stats");

        Assert.True(root.Remove("stats"));
        Assert.False(root.Remove("stats"));
        Assert.False(root.ContainsKey("stats"));

        root.WriteInt("stats", 1);
        Assert.Equal(PrimitiveKind.Int, Primitive(root, "stats").Kind);
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalse()
    {
        Assert.False(new SerializationObject().TryGet("x", out var element));
        Assert.Null(element);
    }
}
