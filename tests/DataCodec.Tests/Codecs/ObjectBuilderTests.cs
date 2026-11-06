using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Codecs;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;
using DataCodec.Tests.TestSupport;

namespace DataCodec.Tests.Codecs;

public class ObjectBuilderTests
{
    private static readonly IDataEncoder Encoder = JsonDataEncoder.Compact;
    private static readonly IDataDecoder Decoder = JsonDataDecoder.Default;

    private static T RoundTrip<T>(ICodec<T> codec, T value) where T : notnull =>
        codec.Decode(codec.EncodeToString(value, Encoder).GetOrThrow(), Decoder).GetOrThrow();

    [Fact]
    public void Object_InfersSubtype_NoCasts()
    {
        IMapCodec<IShape> codec = Codec.For<IShape>().Object(
            Codec.Double.Field("r", (Circle c) => c.R),
            r => new Circle(r));

        Assert.Equal("""{"r":3}""", codec.EncodeToString(new Circle(3), Encoder).GetOrThrow());
        Assert.Equal(new Circle(3), RoundTrip(codec, new Circle(3)));
        Assert.Equal(new[] { "r" }, codec.Keys);
    }

    [Fact]
    public void Object_WrongSubtype_Throws()
    {
        IMapCodec<IShape> codec = Codec.For<IShape>().Object(
            Codec.Double.Field("r", (Circle c) => c.R),
            r => new Circle(r));

        var error = Assert.Throws<DataEncodeException>(() => codec.Encode(new Square(1), new RecordingWriter()));

        Assert.Equal("expected Circle but was Square", error.Reason);
    }

    private interface ICondition
    {
        bool Test(int value);
    }

    private sealed record Above(int Limit) : ICondition
    {
        public bool Test(int value) => value > Limit;
    }

    private sealed record AllOf(ICondition[] Conditions) : ICondition
    {
        public bool Test(int value) => Conditions.All(c => c.Test(value));
    }

    [Fact]
    public void Object_ArrayOfBaseCodec_LikeCompositeCondition()
    {
        ICodec<ICondition> above = Codec.For<ICondition>().Object(
            Codec.Int.Field("limit", (Above a) => a.Limit),
            limit => new Above(limit));
        ICodec<ICondition> allOf = Codec.For<ICondition>().Object(
            above.ArrayOf().Field("conditions", (AllOf a) => a.Conditions),
            conditions => new AllOf(conditions));

        ICondition decoded = allOf.Decode("""{"conditions":[{"limit":1},{"limit":5}]}""", Decoder).GetOrThrow();

        Assert.True(decoded.Test(6));
        Assert.False(decoded.Test(3));
    }

    private sealed record Wide(
        int A, int B, int C, int D, int E, int F, int G, int H,
        int I, int J, int K, int L, int M, int N, int O, int P);

    [Fact]
    public void Object_SixteenFields_RoundTrips()
    {
        IMapCodec<object> codec = Codec.For<object>().Object(
            Codec.Int.Field("a", (Wide w) => w.A), Codec.Int.Field("b", (Wide w) => w.B),
            Codec.Int.Field("c", (Wide w) => w.C), Codec.Int.Field("d", (Wide w) => w.D),
            Codec.Int.Field("e", (Wide w) => w.E), Codec.Int.Field("f", (Wide w) => w.F),
            Codec.Int.Field("g", (Wide w) => w.G), Codec.Int.Field("h", (Wide w) => w.H),
            Codec.Int.Field("i", (Wide w) => w.I), Codec.Int.Field("j", (Wide w) => w.J),
            Codec.Int.Field("k", (Wide w) => w.K), Codec.Int.Field("l", (Wide w) => w.L),
            Codec.Int.Field("m", (Wide w) => w.M), Codec.Int.Field("n", (Wide w) => w.N),
            Codec.Int.Field("o", (Wide w) => w.O), Codec.Int.Field("p", (Wide w) => w.P),
            (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) => new Wide(a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p));
        var wide = new Wide(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);

        Assert.Equal(wide, RoundTrip(codec, wide));
        Assert.Equal(16, codec.Keys.Count);
    }

    [Fact]
    public void From_MapCodec_StaysMapCodec()
    {
        IMapCodec<IShape> codec = Codec.For<IShape>().From(Circle.Codec);

        Assert.Equal(new Circle(2), codec.DecodeFields(Decoder.Decode("""{"r":2}""").GetOrThrow()).GetOrThrow());
    }

    [Fact]
    public void From_PlainCodec()
    {
        ICodec<object> codec = Codec.For<object>().From(Codec.Int);

        Assert.Equal(5, RoundTrip(codec, 5));
    }

    [Fact]
    public void Object_DuplicateKeys_Throws()
    {
        Assert.Throws<ArgumentException>(() => Codec.For<IShape>().Object(
            Codec.Double.Field("r", (Circle c) => c.R),
            Codec.Double.Field("r", (Circle c) => c.R),
            (r, _) => new Circle(r)));
    }
}
