using DataCodec.Api.Serialization;
using DataCodec.Tests.TestSupport;

namespace DataCodec.Tests.Api.Serialization;

public class EncodeTests
{
    [Fact]
    public void ErrorPaths_UseJsonStyle()
    {
        var nested = new SerializationObject();
        var a = nested.WriteArray("a");
        a.AppendInt(0);
        a.AppendInt(1);
        a.AppendObject().WriteDouble("c", 1.5);

        Assert.Equal("$.a[2].c", FailingPath(nested));
        Assert.Equal("$['odd key']", FailingPath(new SerializationObject().WriteDouble("odd key", 1.5)));
        Assert.Equal("$['it\\'s']", FailingPath(new SerializationObject().WriteDouble("it's", 1.5)));

        var rootArray = new SerializationArray();
        rootArray.AppendDouble(1.5);
        Assert.Equal("$[0]", FailingPath(rootArray));
    }

    [Fact]
    public void RemoveAt_ErrorPathUsesCurrentIndex()
    {
        var array = new SerializationArray();
        array.AppendInt(1);
        array.AppendDouble(1.5);
        array.RemoveAt(0);

        Assert.Equal("$[0]", FailingPath(array));
    }

    private static string FailingPath(SerializationElement root) =>
        Assert.Throws<DataEncodeException>(() => root.Encode(new RecordingWriter { FailDoubles = true })).Path;

    [Fact]
    public void Encode_CallsWriterInOrder()
    {
        var root = new SerializationObject().WriteInt("id", 1);
        var tag = root.WriteArray("tag");
        tag.AppendString("x");
        tag.AppendObject().WriteNull("n");
        var writer = new RecordingWriter();

        root.Encode(writer);

        Assert.Equal(
            new[]
            {
                "{", "key:id", "int:1",
                "key:tag", "[", "string:x", "{", "key:n", "null", "}", "]",
                "}",
            },
            writer.Calls);
    }

    [Fact]
    public void Encode_AllPrimitiveKinds()
    {
        var array = new SerializationArray();
        array.AppendInt(1);
        array.AppendLong(2L);
        array.AppendFloat(1.5f);
        array.AppendDouble(2.5);
        array.AppendDecimal(0.1m);
        array.AppendBool(true);
        var writer = new RecordingWriter();

        array.Encode(writer);

        Assert.Equal(
            new[] { "[", "int:1", "long:2", "float:1.5", "double:2.5", "decimal:0.1", "bool:True", "]" },
            writer.Calls);
    }

    [Fact]
    public void Encode_WriterFailure_GivesPathOnError()
    {
        var root = new SerializationObject();
        root.WriteObject("stats").WriteDouble("speed", 1.5);

        var error = Assert.Throws<DataEncodeException>(() => root.Encode(new RecordingWriter { FailDoubles = true }));

        Assert.Equal("$.stats.speed", error.Path);
        Assert.Equal("$.stats.speed: doubles not supported", error.Message);
        Assert.IsType<NotSupportedException>(error.InnerException);
    }
}
