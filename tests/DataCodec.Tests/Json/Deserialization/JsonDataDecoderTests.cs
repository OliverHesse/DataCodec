using System.Text;
using System.Text.Json;
using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;

namespace DataCodec.Tests.Json.Deserialization;

public class JsonDataDecoderTests
{
    private const string Sample = """{ "tag": [12, 23, 43], "id": 1 }""";

    private static readonly IDataDecoder Decoder = JsonDataDecoder.Default;

    private static string TempPath() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"datacodec-{Guid.NewGuid():N}.json");

    [Fact]
    public void Decode_Text_ReturnsDataObject()
    {
        IDataObject data = Decoder.Decode(Sample).GetOrThrow();

        Assert.Equal("json", Decoder.Name);
        Assert.Equal(1, data.GetInt("id").GetOrThrow());
        Assert.Equal(3, data.AsArray("tag").GetOrThrow().Length);
    }

    [Fact]
    public void Decode_InvalidText_Fails()
    {
        var result = Decoder.Decode("{ \"id\": ");

        Assert.IsAssignableFrom<JsonException>(result.Error);
    }

    [Fact]
    public void Decode_Stream_ReturnsDataObject()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Sample));

        Assert.Equal(1, Decoder.Decode(stream).GetOrThrow().GetInt("id").GetOrThrow());
    }

    [Fact]
    public void Lenient_AllowsCommentsAndTrailingCommas()
    {
        const string json = "{ \"id\": 1, // comment\n }";

        Assert.Equal(1, JsonDataDecoder.Lenient.Decode(json).GetOrThrow().GetInt("id").GetOrThrow());
        Assert.True(JsonDataDecoder.Default.Decode(json).IsError);
    }

    [Fact]
    public void DecodeFile_ReadsFile()
    {
        string path = TempPath();
        try
        {
            File.WriteAllText(path, Sample);

            Assert.Equal(1, Decoder.DecodeFile(path).GetOrThrow().GetInt("id").GetOrThrow());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DecodeFile_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => Decoder.DecodeFile(TempPath()));
    }

    [Fact]
    public void DecodeFile_InvalidContent_Fails()
    {
        string path = TempPath();
        try
        {
            File.WriteAllText(path, "not json");

            Assert.IsAssignableFrom<JsonException>(Decoder.DecodeFile(path).Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RoundTrip_EncodeToFileThenDecodeFile()
    {
        var root = new SerializationObject().WriteInt("id", 7).WriteString("name", "sword");
        root.WriteArray("tag").AppendDouble(1.5);
        string path = TempPath();
        try
        {
            root.EncodeToFile(JsonDataEncoder.Indented, path);

            IDataObject back = Decoder.DecodeFile(path).GetOrThrow();
            Assert.Equal(7, back.GetInt("id").GetOrThrow());
            Assert.Equal("sword", back.GetString("name").GetOrThrow());
            Assert.Equal(1.5, back.AsArray("tag").GetOrThrow()[0].GetDouble().GetOrThrow());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
