using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;

namespace DataCodec.Tests.Json.Serialization;

public class JsonDataEncoderTests
{
    private static SerializationObject CreateSample()
    {
        var root = new SerializationObject();
        root.WriteInt("id", 1);

        var tags = root.WriteArray("tag");
        tags.AppendInt(12);
        tags.AppendInt(23);
        tags.AppendInt(43);

        root.WriteObject("stats")
            .WriteDouble("speed", 1.5)
            .WriteBool("flying", false);

        var items = root.WriteArray("items");
        items.AppendObject().WriteString("name", "sword");
        items.AppendString("loose value");
        return root;
    }

    [Fact]
    public void EncodeWithWriterAction_WritesJson()
    {
        IDataEncoder encoder = JsonDataEncoder.Compact;

        var result = encoder.EncodeToString(w =>
        {
            w.WriteStartArray();
            w.WriteInt(1);
            w.WriteEndArray();
        });

        Assert.Equal("[1]", result.GetOrThrow());
    }

    [Fact]
    public void Compact_ExactOutput()
    {
        string json = CreateSample().EncodeToString(JsonDataEncoder.Compact).GetOrThrow();

        Assert.Equal(
            """{"id":1,"tag":[12,23,43],"stats":{"speed":1.5,"flying":false},"items":[{"name":"sword"},"loose value"]}""",
            json);
    }

    [Fact]
    public void Indented_ExactOutput()
    {
        var root = new SerializationObject().WriteInt("id", 1);
        root.WriteArray("tag").AppendInt(12);

        string json = root.EncodeToString(JsonDataEncoder.Indented).GetOrThrow();

        Assert.Equal(
            """
            {
              "id": 1,
              "tag": [
                12
              ]
            }
            """,
            json.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void RoundTrip_ThroughReader()
    {
        var root = new SerializationObject()
            .WriteInt("i", 5)
            .WriteLong("l", 9999999999L)
            .WriteFloat("f", 1.5f)
            .WriteDouble("d", 2.25)
            .WriteDecimal("m", 0.1m)
            .WriteString("s", "hi \"there\"")
            .WriteBool("b", true)
            .WriteNull("n");
        root.WriteArray("a").AppendObject().WriteInt("x", 7);

        string json = root.EncodeToString(JsonDataEncoder.Compact).GetOrThrow();
        IDataObject back = JsonDataObject.Parse(json).GetOrThrow();

        Assert.Equal(5, back.GetInt("i").GetOrThrow());
        Assert.Equal(9999999999L, back.GetLong("l").GetOrThrow());
        Assert.Equal(1.5f, back.GetFloat("f").GetOrThrow());
        Assert.Equal(2.25, back.GetDouble("d").GetOrThrow());
        Assert.Equal(0.1m, back.GetDecimal("m").GetOrThrow());
        Assert.Equal("hi \"there\"", back.GetString("s").GetOrThrow());
        Assert.True(back.GetBool("b").GetOrThrow());
        Assert.True(back.GetDataObject("n").GetOrThrow().IsNull());
        Assert.True(back.GetString("n").IsError);
        Assert.Equal(7, back.AsArray("a").GetOrThrow()[0].GetInt("x").GetOrThrow());
        Assert.Equal(new[] { "i", "l", "f", "d", "m", "s", "b", "n", "a" }, back.Keys);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteDouble_FailsWithPath(double value)
    {
        var root = new SerializationObject();
        root.WriteObject("stats").WriteDouble("speed", value);

        var error = Assert.IsType<DataEncodeException>(root.EncodeToString(JsonDataEncoder.Compact).Error);

        Assert.Equal("$.stats.speed", error.Path);
        Assert.StartsWith("$.stats.speed: ", error.Message);
    }

    [Fact]
    public void NonFiniteFloat_Fails()
    {
        var array = new SerializationArray();
        array.AppendFloat(float.NaN);

        var error = Assert.IsType<DataEncodeException>(array.EncodeToString(JsonDataEncoder.Compact).Error);

        Assert.Equal("$[0]", error.Path);
    }

    [Fact]
    public void RootArray_Encodes()
    {
        var array = new SerializationArray();
        array.AppendInt(1);
        array.AppendArray().AppendInt(2);
        array.AppendNull();

        Assert.Equal("[1,[2],null]", array.EncodeToString(JsonDataEncoder.Compact).GetOrThrow());
    }

    [Fact]
    public void EncodeTo_Stream()
    {
        using var stream = new MemoryStream();

        CreateSample().EncodeTo(JsonDataEncoder.Compact, stream);

        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void EncodeTo_Stream_NonFinite_Throws()
    {
        var root = new SerializationObject();
        root.WriteObject("stats").WriteDouble("speed", double.NaN);
        using var stream = new MemoryStream();

        var error = Assert.Throws<DataEncodeException>(() => root.EncodeTo(JsonDataEncoder.Compact, stream));

        Assert.Equal("$.stats.speed", error.Path);
    }

    [Fact]
    public void EncodeToFile_WritesAndReadsBack()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"datacodec-{Guid.NewGuid():N}.json");
        try
        {
            CreateSample().EncodeToFile(JsonDataEncoder.Indented, path);

            IDataObject back = JsonDataObject.Parse(File.ReadAllText(path)).GetOrThrow();
            Assert.Equal(1, back.GetInt("id").GetOrThrow());
            Assert.Equal("sword", back.AsArray("items").GetOrThrow()[0].GetString("name").GetOrThrow());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void EncodeToFile_MissingDirectory_Throws()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"missing-{Guid.NewGuid():N}", "out.json");

        Assert.Throws<DirectoryNotFoundException>(() => CreateSample().EncodeToFile(JsonDataEncoder.Compact, path));
    }

    [Fact]
    public void EncodeToFile_EncodeFailure_WritesNoFile()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"datacodec-{Guid.NewGuid():N}.json");
        var root = new SerializationObject().WriteDouble("x", double.NaN);

        Assert.Throws<DataEncodeException>(() => root.EncodeToFile(JsonDataEncoder.Compact, path));
        Assert.False(File.Exists(path));
    }
}
