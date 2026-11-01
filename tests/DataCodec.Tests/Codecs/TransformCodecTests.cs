using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Codecs;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;
using DataCodec.Tests.TestSupport;

namespace DataCodec.Tests.Codecs;

public class TransformCodecTests
{
    private static readonly IDataEncoder Encoder = JsonDataEncoder.Compact;
    private static readonly IDataDecoder Decoder = JsonDataDecoder.Default;

    private static IDataObject Json(string text) => Decoder.Decode(text).GetOrThrow();

    [Fact]
    public void Xmap_Guid_RoundTrips()
    {
        var codec = Codec.String.Xmap(Guid.Parse, g => g.ToString());
        var id = Guid.NewGuid();

        string text = codec.EncodeToString(id, Encoder).GetOrThrow();

        Assert.Equal($"\"{id}\"", text);
        Assert.Equal(id, codec.Decode(text, Decoder).GetOrThrow());
    }

    [Fact]
    public void Xmap_DecodeThrows_Fails()
    {
        var codec = Codec.String.Xmap(Guid.Parse, g => g.ToString());

        var error = Assert.IsType<DataDecodeException>(codec.Decode(Json("\"nope\"")).Error);

        Assert.Equal("$", error.Path);
        Assert.IsType<FormatException>(error.InnerException);
    }

    [Fact]
    public void Xmap_DecodeReturnsNull_Fails()
    {
        var codec = Codec.String.Xmap<string, string>(_ => null!, s => s);

        var error = Assert.IsType<DataDecodeException>(codec.Decode(Json("\"a\"")).Error);

        Assert.Equal("transform returned null", error.Reason);
    }

    [Fact]
    public void Xmap_EncodeThrows_IsWrapped()
    {
        var codec = Codec.String.Xmap<string, string>(s => s, _ => throw new InvalidOperationException("no"));

        var error = Assert.Throws<DataEncodeException>(() => codec.Encode("a", new RecordingWriter()));

        Assert.Equal("$: no", error.Message);
        Assert.IsType<InvalidOperationException>(error.InnerException);
    }

    [Fact]
    public void Validate_FailsWithReason()
    {
        var codec = Codec.String.Validate(s => s.Length > 0, "must not be empty");

        Assert.Equal("$: must not be empty", codec.Decode(Json("\"\"")).Error!.Message);
        Assert.Equal("ok", codec.Decode(Json("\"ok\"")).GetOrThrow());
    }

    [Fact]
    public void Validate_PredicateThrows_Fails()
    {
        var codec = Codec.String.Validate(_ => throw new InvalidOperationException("boom"), "never");

        var error = Assert.IsType<DataDecodeException>(codec.Decode(Json("\"x\"")).Error);

        Assert.IsType<InvalidOperationException>(error.InnerException);
    }

    [Fact]
    public void Validate_Encode_Throws()
    {
        var codec = Codec.String.Validate(s => s.Length > 0, "must not be empty");

        var error = Assert.Throws<DataEncodeException>(() => codec.Encode("", new RecordingWriter()));

        Assert.Equal("must not be empty", error.Reason);
    }

    [Fact]
    public void Min_Max_Range_Messages()
    {
        Assert.Equal("$: -2 is below the minimum 0", Codec.Int.Min(0).Decode(Json("-2")).Error!.Message);
        Assert.Equal("$: 100 is above the maximum 99", Codec.Int.Max(99).Decode(Json("100")).Error!.Message);

        var range = Codec.Int.Range(0, 99);
        Assert.Equal("$: -2 is below the minimum 0", range.Decode(Json("-2")).Error!.Message);
        Assert.Equal("$: 100 is above the maximum 99", range.Decode(Json("100")).Error!.Message);
        Assert.Equal(50, range.Decode(Json("50")).GetOrThrow());
    }

    [Fact]
    public void Clamp_ClampsBothDirections()
    {
        var codec = Codec.Int.Clamp(0, 10);
        var writer = new RecordingWriter();

        codec.Encode(-3, writer);

        Assert.Equal(10, codec.Decode(Json("15")).GetOrThrow());
        Assert.Equal(new[] { "int:0" }, writer.Calls);
    }

    [Fact]
    public void Min_ReasonThrows_StillFails()
    {
        var codec = Codec.Int.Xmap(i => new Unprintable(i), u => u.Value).Min(new Unprintable(5));

        var error = Assert.IsType<DataDecodeException>(codec.Decode(Json("1")).Error);

        Assert.IsType<InvalidOperationException>(error.InnerException);
    }

    private readonly record struct Unprintable(int Value) : IComparable<Unprintable>
    {
        public int CompareTo(Unprintable other) => Value.CompareTo(other.Value);

        public override string ToString() => throw new InvalidOperationException("cannot print");
    }

    [Fact]
    public void Range_MinAboveMax_Throws()
    {
        Assert.Throws<ArgumentException>(() => Codec.Int.Range(5, 1));
        Assert.Throws<ArgumentException>(() => Codec.Int.Clamp(5, 1));
    }
}
