using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Codecs;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;

namespace DataCodec.Tests.Codecs;

public class CollectionCodecTests
{
    private static readonly IDataEncoder Encoder = JsonDataEncoder.Compact;
    private static readonly IDataDecoder Decoder = JsonDataDecoder.Default;

    private static DataEncodeException EncodeFailure<T>(ICodec<T> codec, T value) where T : notnull
    {
        using var stream = new MemoryStream();
        return Assert.Throws<DataEncodeException>(() => codec.EncodeTo(value, Encoder, stream));
    }

    [Fact]
    public void List_RoundTrips()
    {
        var codec = Codec.Int.ListOf();

        string text = codec.EncodeToString([1, 2, 3], Encoder).GetOrThrow();

        Assert.Equal("[1,2,3]", text);
        Assert.Equal(new[] { 1, 2, 3 }, codec.Decode(text, Decoder).GetOrThrow());
    }

    [Fact]
    public void List_ItemFailure_HasIndexPath()
    {
        var error = Codec.Int.ListOf().Decode("[1, \"x\"]", Decoder).Error;

        Assert.Equal("$[1]: expected Number but was String", error!.Message);
    }

    [Fact]
    public void List_NotArray_Fails()
    {
        Assert.True(Codec.Int.ListOf().Decode("{}", Decoder).IsError);
    }

    [Fact]
    public void List_Encode_NaNItem_PathHasIndex()
    {
        var error = EncodeFailure(Codec.Double.ListOf(), [1.0, double.NaN]);

        Assert.Equal("$[1]", error.Path);
    }

    [Fact]
    public void Array_RoundTrips()
    {
        var codec = Codec.Int.ArrayOf();

        string text = codec.EncodeToString([1, 2, 3], Encoder).GetOrThrow();

        Assert.Equal("[1,2,3]", text);
        Assert.Equal(new[] { 1, 2, 3 }, codec.Decode(text, Decoder).GetOrThrow());
    }

    [Fact]
    public void Array_Empty_RoundTrips()
    {
        var codec = Codec.Int.ArrayOf();

        Assert.Equal("[]", codec.EncodeToString([], Encoder).GetOrThrow());
        Assert.Empty(codec.Decode("[]", Decoder).GetOrThrow());
    }

    [Fact]
    public void Array_ItemFailure_HasIndexPath()
    {
        var error = Codec.Int.ArrayOf().Decode("[1, \"x\"]", Decoder).Error;

        Assert.Equal("$[1]: expected Number but was String", error!.Message);
    }

    [Fact]
    public void Array_NotArray_Fails()
    {
        Assert.True(Codec.Int.ArrayOf().Decode("{}", Decoder).IsError);
    }

    [Fact]
    public void Array_Encode_NullItem_PathHasIndex()
    {
        var error = EncodeFailure(Codec.String.ArrayOf(), ["a", null!]);

        Assert.Equal("$[1]", error.Path);
        Assert.Equal("item is null", error.Reason);
    }

    [Fact]
    public void Array_Encode_NaNItem_PathHasIndex()
    {
        var error = EncodeFailure(Codec.Double.ArrayOf(), [1.0, double.NaN]);

        Assert.Equal("$[1]", error.Path);
    }

    [Fact]
    public void Dictionary_RoundTrips()
    {
        var codec = Codec.Int.DictionaryOf();

        string text = codec.EncodeToString(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }, Encoder).GetOrThrow();
        var back = codec.Decode(text, Decoder).GetOrThrow();

        Assert.Equal("""{"a":1,"b":2}""", text);
        Assert.Equal(new[] { "a", "b" }, back.Keys);
        Assert.Equal(2, back["b"]);
    }

    [Fact]
    public void Dictionary_DuplicateKeys_LastWins()
    {
        var result = Codec.Int.DictionaryOf().Decode("""{"a":1,"a":2}""", Decoder);

        Assert.Equal(2, result.GetOrThrow()["a"]);
    }

    [Fact]
    public void Dictionary_NotObject_Fails()
    {
        var error = Codec.Int.DictionaryOf().Decode("[1]", Decoder).Error;

        Assert.Equal("$: expected Object but was Array", error!.Message);
    }

    [Fact]
    public void Dictionary_ValueFailure_HasKeyPath()
    {
        var error = Codec.Int.DictionaryOf().Decode("""{"a":"x"}""", Decoder).Error;

        Assert.Equal("$.a: expected Number but was String", error!.Message);
    }

    [Fact]
    public void Nested_EncodePath_UsesBracketKeys()
    {
        var codec = Codec.Double.ListOf().DictionaryOf();

        var error = EncodeFailure(codec, new Dictionary<string, List<double>> { ["odd key"] = [double.NaN] });

        Assert.Equal("$['odd key'][0]", error.Path);
    }
}
