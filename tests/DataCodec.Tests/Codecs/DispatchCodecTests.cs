using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Codecs;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;
using DataCodec.Tests.TestSupport;

namespace DataCodec.Tests.Codecs;

public class DispatchCodecTests
{
    private static readonly IDataEncoder Encoder = JsonDataEncoder.Compact;
    private static readonly IDataDecoder Decoder = JsonDataDecoder.Default;

    private interface IThing;

    private sealed record Person(string Name) : IThing;

    private sealed record Box(int Size, bool Open) : IThing;

    private sealed record Stray : IThing;

    private sealed record Holder(string Label, IThing Thing);

    private static readonly IMapCodec<IThing> PersonCodec = Codec.For<IThing>().Object(
        Codec.String.Field("name", (Person p) => p.Name),
        name => new Person(name));

    private static readonly IMapCodec<IThing> BoxCodec = Codec.For<IThing>().Object(
        Codec.Int.Field("size", (Box b) => b.Size),
        Codec.Bool.Optional("open", false, (Box b) => b.Open),
        (size, open) => new Box(size, open));

    private static string KeyOf(IThing thing) => thing switch
    {
        Person => "core:example",
        Box => "core:box",
        _ => "core:stray",
    };

    private static (ICodec<IThing> Codec, Dictionary<string, IMapCodec<IThing>> Types) Create(string typeKey = "type")
    {
        var types = new Dictionary<string, IMapCodec<IThing>>
        {
            ["core:example"] = PersonCodec,
            ["core:box"] = BoxCodec,
        };
        ICodec<IThing> codec = Codec.Dispatch<IThing, string>(Codec.String, KeyOf, types.GetValueOrDefault, typeKey);
        return (codec, types);
    }

    private static string Encode<T>(ICodec<T> codec, T value) where T : notnull =>
        codec.EncodeToString(value, Encoder).GetOrThrow();

    [Fact]
    public void Encode_FlatObject_TypeFirst()
    {
        Assert.Equal("""{"type":"core:example","name":"John"}""", Encode(Create().Codec, new Person("John")));
    }

    [Fact]
    public void RoundTrip_EachType()
    {
        ICodec<IThing> codec = Create().Codec;

        foreach (IThing thing in new IThing[] { new Person("John"), new Box(3, true) })
        {
            Assert.Equal(thing, codec.Decode(Encode(codec, thing), Decoder).GetOrThrow());
        }
    }

    [Fact]
    public void Decode_HandWritten_UsesDefaults()
    {
        IThing thing = Create().Codec.Decode("""{ "size": 4, "type": "core:box" }""", Decoder).GetOrThrow();

        Assert.Equal(new Box(4, false), thing);
    }

    [Fact]
    public void Decode_UnknownType_FailsAtTypePath()
    {
        var error = Assert.IsType<DataDecodeException>(Create().Codec.Decode("""{"type":"core:nope"}""", Decoder).Error);

        Assert.Equal("$.type", error.Path);
        Assert.Equal("unknown type 'core:nope'", error.Reason);
    }

    [Fact]
    public void Decode_MissingType_Fails()
    {
        var error = Assert.IsType<DataDecodeException>(Create().Codec.Decode("""{"name":"John"}""", Decoder).Error);

        Assert.Equal("$: missing 'type' key", error.Message);
    }

    [Fact]
    public void Decode_TypeNotString_Fails()
    {
        Assert.True(Create().Codec.Decode("""{"type":5}""", Decoder).IsError);
    }

    [Fact]
    public void Decode_NotObject_Fails()
    {
        Assert.True(Create().Codec.Decode("\"core:example\"", Decoder).IsError);
    }

    [Fact]
    public void Decode_SubFieldError_HasPath()
    {
        DataResult<IThing> result = Create().Codec.Decode("""{"type":"core:box","size":"big"}""", Decoder);

        Assert.StartsWith("$.size:", result.Error!.Message);
    }

    [Fact]
    public void Nested_DecodeErrorPathIncludesParent()
    {
        IMapCodec<Holder> holder = Codec.Object(
            Codec.String.Field("label", (Holder h) => h.Label),
            Create().Codec.Field("thing", (Holder h) => h.Thing),
            (label, thing) => new Holder(label, thing));

        DataResult<Holder> result = holder.Decode("""{"label":"x","thing":{"type":"core:box","size":"big"}}""", Decoder);

        Assert.StartsWith("$.thing.size:", result.Error!.Message);
    }

    [Fact]
    public void Nested_RoundTrips()
    {
        IMapCodec<Holder> holder = Codec.Object(
            Codec.String.Field("label", (Holder h) => h.Label),
            Create().Codec.ArrayOf().Field("things", (Holder h) => new[] { h.Thing }),
            (label, things) => new Holder(label, things[0]));
        var value = new Holder("crate", new Box(2, false));

        string text = Encode(holder, value);

        Assert.Equal("""{"label":"crate","things":[{"type":"core:box","size":2,"open":false}]}""", text);
        Assert.Equal(value, holder.Decode(text, Decoder).GetOrThrow());
    }

    [Fact]
    public void Encode_UnknownKey_Throws()
    {
        var error = Assert.Throws<DataEncodeException>(() => Create().Codec.Encode(new Stray(), new RecordingWriter()));

        Assert.Equal("$.type", error.Path);
        Assert.Equal("no codec for type 'core:stray'", error.Reason);
    }

    [Fact]
    public void Encode_KeyOfThrows_BecomesEncodeException()
    {
        ICodec<IThing> codec = Codec.Dispatch<IThing, string>(
            Codec.String, _ => throw new InvalidOperationException("no key"), _ => PersonCodec);

        var error = Assert.Throws<DataEncodeException>(() => codec.Encode(new Person("x"), new RecordingWriter()));

        Assert.Equal("no key", error.Reason);
    }

    [Fact]
    public void Encode_NestedError_PathIncludesParent()
    {
        var (codec, types) = Create();
        types["core:box"] = Codec.For<IThing>().Object(
            Codec.Double.Field("size", (Box b) => double.NaN),
            size => new Box((int)size, false));

        var error = Assert.Throws<DataEncodeException>(() => Encode(
            Codec.Object(codec.Field("thing", (Holder h) => h.Thing), t => new Holder("", t)),
            new Holder("", new Box(1, false))));

        Assert.Equal("$.thing.size", error.Path);
    }

    [Fact]
    public void TypeKeyCollision_FailsBothWays()
    {
        var (codec, types) = Create();
        types["core:example"] = Codec.For<IThing>().Object(
            Codec.String.Field("type", (Person p) => p.Name),
            name => new Person(name));

        DataResult<IThing> decoded = codec.Decode("""{"type":"core:example"}""", Decoder);
        var encodeError = Assert.Throws<DataEncodeException>(() => codec.Encode(new Person("x"), new RecordingWriter()));

        Assert.Contains("has its own 'type' field", decoded.Error!.Message);
        Assert.Contains("has its own 'type' field", encodeError.Reason);
    }

    [Fact]
    public void CustomTypeKey_IsUsed()
    {
        ICodec<IThing> codec = Create("kind").Codec;

        string text = Encode(codec, new Person("John"));

        Assert.Equal("""{"kind":"core:example","name":"John"}""", text);
        Assert.Equal(new Person("John"), codec.Decode(text, Decoder).GetOrThrow());
    }

    [Fact]
    public void Lookup_IsLazy()
    {
        var types = new Dictionary<string, IMapCodec<IThing>>();
        ICodec<IThing> codec = Codec.Dispatch<IThing, string>(Codec.String, KeyOf, types.GetValueOrDefault);

        types["core:example"] = PersonCodec;

        Assert.Equal(new Person("Ada"), codec.Decode(Encode(codec, new Person("Ada")), Decoder).GetOrThrow());
    }

    private enum Kind
    {
        Person,
        Box,
    }

    [Fact]
    public void EnumKeys_WithSwitch()
    {
        ICodec<IThing> codec = Codec.Dispatch<IThing, Kind>(
            Codec.Enum<Kind>(),
            thing => thing is Person ? Kind.Person : Kind.Box,
            kind => kind switch { Kind.Person => PersonCodec, Kind.Box => BoxCodec, _ => null });

        Assert.Equal("""{"type":"Box","size":1,"open":true}""", Encode(codec, new Box(1, true)));
    }

    [Fact]
    public void Dispatch_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => Codec.Dispatch<IThing, string>(null!, KeyOf, _ => null));
        Assert.Throws<ArgumentException>(() => Codec.Dispatch<IThing, string>(Codec.String, KeyOf, _ => null, ""));
    }
}
