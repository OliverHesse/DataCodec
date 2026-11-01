using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Codecs;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;
using DataCodec.Tests.TestSupport;

namespace DataCodec.Tests.Codecs;

public class EnumCodecTests
{
    private enum Color
    {
        Red,
        Green,
        Blue,
    }

    private static readonly IDataEncoder Encoder = JsonDataEncoder.Compact;
    private static readonly IDataDecoder Decoder = JsonDataDecoder.Default;

    [Fact]
    public void Enum_RoundTrips()
    {
        var codec = Codec.Enum<Color>();

        string text = codec.EncodeToString(Color.Green, Encoder).GetOrThrow();

        Assert.Equal("\"Green\"", text);
        Assert.Equal(Color.Green, codec.Decode(text, Decoder).GetOrThrow());
    }

    [Theory]
    [InlineData("\"Purple\"")]
    [InlineData("\"red\"")]
    [InlineData("\"1\"")]
    [InlineData("\"Red, Green\"")]
    [InlineData("1")]
    public void Enum_RejectsNonNames(string json)
    {
        Assert.True(Codec.Enum<Color>().Decode(json, Decoder).IsError);
    }

    [Fact]
    public void Enum_UnknownName_Message()
    {
        var error = Assert.IsType<DataDecodeException>(Codec.Enum<Color>().Decode("\"Purple\"", Decoder).Error);

        Assert.Equal("$: 'Purple' is not a valid Color", error.Message);
    }

    [Fact]
    public void Enum_EncodeUnnamed_Throws()
    {
        var error = Assert.Throws<DataEncodeException>(() => Codec.Enum<Color>().Encode((Color)42, new RecordingWriter()));

        Assert.Equal("42 is not a named Color value", error.Reason);
    }
}
