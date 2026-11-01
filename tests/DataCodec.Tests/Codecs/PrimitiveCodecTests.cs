using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Codecs;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;
using DataCodec.Tests.TestSupport;

namespace DataCodec.Tests.Codecs;

public class PrimitiveCodecTests
{
    private static readonly IDataEncoder Encoder = JsonDataEncoder.Compact;
    private static readonly IDataDecoder Decoder = JsonDataDecoder.Default;

    private static T RoundTrip<T>(ICodec<T> codec, T value) where T : notnull
    {
        string text = codec.EncodeToString(value, Encoder).GetOrThrow();
        return codec.Decode(text, Decoder).GetOrThrow();
    }

    [Fact]
    public void Primitives_RoundTrip()
    {
        Assert.Equal(5, RoundTrip(Codec.Int, 5));
        Assert.Equal(9999999999L, RoundTrip(Codec.Long, 9999999999L));
        Assert.Equal(1.5f, RoundTrip(Codec.Float, 1.5f));
        Assert.Equal(2.25, RoundTrip(Codec.Double, 2.25));
        Assert.Equal(0.1m, RoundTrip(Codec.Decimal, 0.1m));
        Assert.Equal("hi", RoundTrip(Codec.String, "hi"));
        Assert.True(RoundTrip(Codec.Bool, true));
    }

    [Fact]
    public void Primitive_IsStrict()
    {
        var error = Codec.Int.Decode("\"12\"", Decoder).Error;

        Assert.IsType<InvalidCastException>(error);
        Assert.Equal("$: expected Number but was String", error!.Message);
    }

    [Fact]
    public void Primitive_Encode_WritesValue()
    {
        var writer = new RecordingWriter();

        Codec.Int.Encode(7, writer);

        Assert.Equal(new[] { "int:7" }, writer.Calls);
    }

    [Fact]
    public void Double_NaN_EncodeToString_Fails()
    {
        var error = Assert.IsType<DataEncodeException>(Codec.Double.EncodeToString(double.NaN, Encoder).Error);

        Assert.Equal("$", error.Path);
    }

    [Fact]
    public void EncodeToFile_ThenDecodeFile_RoundTrips()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"datacodec-{Guid.NewGuid():N}.json");
        try
        {
            Codec.String.EncodeToFile("saved", Encoder, path);

            Assert.Equal("saved", Codec.String.DecodeFile(Decoder, path).GetOrThrow());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void EncodeTo_Stream_WritesJson()
    {
        using var stream = new MemoryStream();

        Codec.Bool.EncodeTo(false, Encoder, stream);

        Assert.Equal("false", System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }
}
