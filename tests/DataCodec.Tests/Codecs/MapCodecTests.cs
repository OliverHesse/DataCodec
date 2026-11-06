using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Codecs;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;
using DataCodec.Tests.TestSupport;

namespace DataCodec.Tests.Codecs;

public class MapCodecTests
{
    private static readonly IDataEncoder Encoder = JsonDataEncoder.Compact;
    private static readonly IDataDecoder Decoder = JsonDataDecoder.Default;

    private sealed record Stats(double Speed, int Hp);

    private static readonly IMapCodec<Stats> StatsCodec = Codec.Object(
        Codec.Double.Field("speed", (Stats s) => s.Speed),
        Codec.Int.Field("hp", (Stats s) => s.Hp),
        (speed, hp) => new Stats(speed, hp));

    private static IDataObject Json(string text) => Decoder.Decode(text).GetOrThrow();

    [Fact]
    public void Keys_InDeclarationOrder()
    {
        Assert.Equal(new[] { "speed", "hp" }, StatsCodec.Keys);
    }

    [Fact]
    public void EncodeFields_WritesNoBraces()
    {
        var writer = new RecordingWriter();

        StatsCodec.EncodeFields(new Stats(1.5, 3), writer);

        Assert.Equal(new[] { "key:speed", "double:1.5", "key:hp", "int:3" }, writer.Calls);
    }

    [Fact]
    public void Encode_StillWritesWholeObject()
    {
        Assert.Equal("""{"speed":1.5,"hp":3}""", StatsCodec.EncodeToString(new Stats(1.5, 3), Encoder).GetOrThrow());
    }

    [Fact]
    public void DecodeFields_IgnoresUnknownKeys()
    {
        Stats stats = StatsCodec.DecodeFields(Json("""{"type":"x","speed":1.5,"hp":3}""")).GetOrThrow();

        Assert.Equal(new Stats(1.5, 3), stats);
    }

    [Fact]
    public void DecodeFields_FieldError_HasPath()
    {
        DataResult<Stats> result = StatsCodec.DecodeFields(Json("""{"speed":1.5,"hp":"lots"}"""));

        Assert.Contains("$.hp", result.Error!.Message);
    }

    [Fact]
    public void Decode_NotObject_StillFails()
    {
        Assert.True(StatsCodec.Decode(Json("5")).IsError);
    }

    [Fact]
    public void Xmap_OnMapCodec_StaysMapCodec()
    {
        IMapCodec<int> hpOnly = StatsCodec.Xmap(s => s.Hp, hp => new Stats(0, hp));

        Assert.Equal(new[] { "speed", "hp" }, hpOnly.Keys);
        Assert.Equal("""{"speed":0,"hp":7}""", hpOnly.EncodeToString(7, Encoder).GetOrThrow());
        Assert.Equal(7, hpOnly.DecodeFields(Json("""{"speed":0,"hp":7}""")).GetOrThrow());
    }

    [Fact]
    public void Xmap_OnMapCodec_EncodeFieldsConverts()
    {
        IMapCodec<int> hpOnly = StatsCodec.Xmap(s => s.Hp, hp => new Stats(0, hp));
        var writer = new RecordingWriter();

        hpOnly.EncodeFields(7, writer);

        Assert.Equal(new[] { "key:speed", "double:0", "key:hp", "int:7" }, writer.Calls);
    }

    [Fact]
    public void Xmap_OnMapCodec_DecodeThrows_FailsAtObjectPath()
    {
        IMapCodec<int> failing = StatsCodec.Xmap<Stats, int>(_ => throw new InvalidOperationException("nope"), _ => new Stats(0, 0));

        DataResult<int> result = failing.DecodeFields(Json("""{"speed":0,"hp":7}"""));

        var error = Assert.IsType<DataDecodeException>(result.Error);
        Assert.Equal("$", error.Path);
    }

    [Fact]
    public void Upcast_OnMapCodec_StaysMapCodec()
    {
        IMapCodec<IShape> shape = Circle.Codec.Upcast<Circle, IShape>();
        var writer = new RecordingWriter();

        shape.EncodeFields(new Circle(2), writer);

        Assert.Equal(new[] { "r" }, shape.Keys);
        Assert.Equal(new[] { "key:r", "double:2" }, writer.Calls);
        Assert.Equal(new Circle(2), shape.DecodeFields(Json("""{"r":2}""")).GetOrThrow());
    }

    [Fact]
    public void Upcast_OnMapCodec_WrongSubtype_Throws()
    {
        IMapCodec<IShape> shape = Circle.Codec.Upcast<Circle, IShape>();

        Assert.Throws<DataEncodeException>(() => shape.EncodeFields(new Square(1), new RecordingWriter()));
    }

    [Fact]
    public void Validate_OnMapCodec_IsPlainCodec()
    {
        ICodec<Stats> validated = StatsCodec.Validate(s => s.Hp > 0, "hp must be positive");

        Assert.IsNotAssignableFrom<IMapCodec<Stats>>(validated);
    }
}
