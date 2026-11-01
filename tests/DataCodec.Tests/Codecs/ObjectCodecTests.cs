using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Codecs;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;
using DataCodec.Tests.TestSupport;

namespace DataCodec.Tests.Codecs;

public class ObjectCodecTests
{
    private enum Color
    {
        Red,
        Green,
        Blue,
    }

    private sealed record Player(int Id, string Name, int Lives, List<Color> Colors, Dictionary<string, int> Scores);

    private sealed record Stats(double Speed);

    private sealed record Unit(string Name, Stats Stats);

    private sealed record Wide(
        int A, int B, int C, int D, int E, int F, int G, int H,
        int I, int J, int K, int L, int M, int N, int O, int P);

    private static readonly ICodec<Player> PlayerCodec = Codec.Object(
        Codec.Int.Field("id", (Player p) => p.Id),
        Codec.String.Validate(s => s.Length > 0, "must not be empty").Field("name", (Player p) => p.Name),
        Codec.Int.Range(0, 99).Optional("lives", 3, (Player p) => p.Lives),
        Codec.Enum<Color>().ListOf().Optional("colors", () => [], (Player p) => p.Colors),
        Codec.Int.DictionaryOf().Field("scores", (Player p) => p.Scores),
        (id, name, lives, colors, scores) => new Player(id, name, lives, colors, scores));

    private static readonly ICodec<Unit> UnitCodec = Codec.Object(
        Codec.String.Field("name", (Unit u) => u.Name),
        Codec.Object(Codec.Double.Field("speed", (Stats s) => s.Speed), speed => new Stats(speed))
            .Field("stats", (Unit u) => u.Stats),
        (name, stats) => new Unit(name, stats));

    private static readonly IDataEncoder Encoder = JsonDataEncoder.Indented;
    private static readonly IDataDecoder Decoder = JsonDataDecoder.Default;

    private static DataResult<Player> DecodePlayer(string json) => PlayerCodec.Decode(json, Decoder);

    [Fact]
    public void Player_RoundTrips()
    {
        var player = new Player(7, "ada", 5, [Color.Red, Color.Blue], new Dictionary<string, int> { ["level1"] = 10 });

        string text = PlayerCodec.EncodeToString(player, Encoder).GetOrThrow();
        Player back = PlayerCodec.Decode(text, Decoder).GetOrThrow();

        Assert.Equal(7, back.Id);
        Assert.Equal("ada", back.Name);
        Assert.Equal(5, back.Lives);
        Assert.Equal(player.Colors, back.Colors);
        Assert.Equal(player.Scores, back.Scores);
    }

    [Fact]
    public void Optional_MissingOrNull_UsesDefault()
    {
        Player missing = DecodePlayer("""{"id":1,"name":"a","scores":{}}""").GetOrThrow();
        Player nulled = DecodePlayer("""{"id":1,"name":"a","lives":null,"scores":{}}""").GetOrThrow();

        Assert.Equal(3, missing.Lives);
        Assert.Empty(missing.Colors);
        Assert.Equal(3, nulled.Lives);
    }

    [Fact]
    public void Optional_FactoryDefault_IsFreshPerDecode()
    {
        Player first = DecodePlayer("""{"id":1,"name":"a","scores":{}}""").GetOrThrow();
        first.Colors.Add(Color.Red);

        Player second = DecodePlayer("""{"id":2,"name":"b","scores":{}}""").GetOrThrow();

        Assert.Empty(second.Colors);
        Assert.NotSame(first.Colors, second.Colors);
    }

    [Fact]
    public void Optional_WrongType_Fails()
    {
        var error = DecodePlayer("""{"id":1,"name":"a","lives":"three","scores":{}}""").Error;

        Assert.Equal("$.lives: expected Number but was String", error!.Message);
    }

    [Fact]
    public void Field_Missing_Fails()
    {
        var error = DecodePlayer("""{"name":"a","scores":{}}""").Error;

        Assert.IsType<KeyNotFoundException>(error);
        Assert.Equal("$: key 'id' not found", error!.Message);
    }

    [Fact]
    public void Field_Null_Fails()
    {
        var error = Assert.IsType<DataDecodeException>(DecodePlayer("""{"id":null,"name":"a","scores":{}}""").Error);

        Assert.Equal("$.id: value is null", error.Message);
    }

    [Fact]
    public void UnknownKeys_Ignored()
    {
        Assert.True(DecodePlayer("""{"id":1,"name":"a","scores":{},"x":1}""").IsSuccess);
    }

    [Fact]
    public void Range_AppliesInObject()
    {
        var error = DecodePlayer("""{"id":1,"name":"a","lives":100,"scores":{}}""").Error;

        Assert.Equal("$.lives: 100 is above the maximum 99", error!.Message);
    }

    [Fact]
    public void Supplier_Throws_Fails()
    {
        var codec = Codec.Object(Codec.Int.Field("id", (Stats s) => 0), Stats (_) => throw new ArgumentException("bad"));

        var error = Assert.IsType<DataDecodeException>(codec.Decode("""{"id":1}""", Decoder).Error);

        Assert.Equal("$", error.Path);
        Assert.Equal("bad", error.Reason);
        Assert.IsType<ArgumentException>(error.InnerException);
    }

    [Fact]
    public void Supplier_ReturnsNull_Fails()
    {
        var codec = Codec.Object(Codec.Int.Field("id", (Stats s) => 0), _ => (Stats)null!);

        var error = Assert.IsType<DataDecodeException>(codec.Decode("""{"id":1}""", Decoder).Error);

        Assert.Equal("supplier returned null", error.Reason);
    }

    [Fact]
    public void NotObject_Fails()
    {
        var error = DecodePlayer("[1]").Error;

        Assert.Equal("$: expected Object but was Array", error!.Message);
    }

    [Fact]
    public void DuplicateFieldKeys_Throw()
    {
        Assert.Throws<ArgumentException>(() => Codec.Object(
            Codec.Int.Field("id", (Player p) => p.Id),
            Codec.Int.Field("id", (Player p) => p.Lives),
            (a, b) => new Player(a, "x", b, [], [])));
    }

    [Fact]
    public void Encode_NestedNaN_FullPath()
    {
        using var stream = new MemoryStream();

        var error = Assert.Throws<DataEncodeException>(
            () => UnitCodec.EncodeTo(new Unit("u", new Stats(double.NaN)), Encoder, stream));

        Assert.Equal("$.stats.speed", error.Path);
        Assert.StartsWith("$.stats.speed: ", error.Message);
    }

    [Fact]
    public void Encode_GetterReturnsNull_Throws()
    {
        var error = Assert.Throws<DataEncodeException>(
            () => UnitCodec.Encode(new Unit(null!, new Stats(1)), new RecordingWriter()));

        Assert.Equal("$.name", error.Path);
        Assert.Equal("getter returned null", error.Reason);
    }

    [Fact]
    public void Encode_WriterCalls()
    {
        var codec = Codec.Object(
            Codec.Int.Field("id", (Unit u) => 1),
            Codec.String.Field("name", (Unit u) => u.Name),
            (_, name) => new Unit(name, new Stats(0)));
        var writer = new RecordingWriter();

        codec.Encode(new Unit("a", new Stats(0)), writer);

        Assert.Equal(new[] { "{", "key:id", "int:1", "key:name", "string:a", "}" }, writer.Calls);
    }

    [Fact]
    public void Object_16Fields_RoundTrips()
    {
        var codec = Codec.Object(
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

        string text = codec.EncodeToString(wide, Encoder).GetOrThrow();

        Assert.Equal(wide, codec.Decode(text, Decoder).GetOrThrow());
    }
}
