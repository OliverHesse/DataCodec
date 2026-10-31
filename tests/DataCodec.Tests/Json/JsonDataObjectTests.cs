using System.Text;
using System.Text.Json;
using DataCodec.Api;
using DataCodec.Json;

namespace DataCodec.Tests.Json;

public class JsonDataObjectTests
{
    private const string Sample = """{ "tag": [12, 23, 43], "id": 1 }""";

    private static IDataObject Parse(string json) => JsonDataObject.Parse(json).GetOrThrow();

    private static string PathOf(IDataObject data) => ((JsonDataObject)data).Path;

    [Fact]
    public void Parse_Valid_Succeeds()
    {
        var result = JsonDataObject.Parse(Sample);

        Assert.True(result.IsSuccess);
        Assert.Equal("$", result.Value!.Path);
    }

    [Fact]
    public void Parse_Invalid_FailsWithJsonException()
    {
        var result = JsonDataObject.Parse("{ \"id\": ");

        Assert.IsAssignableFrom<JsonException>(result.Error);
    }

    [Fact]
    public void Parse_Stream_Succeeds()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Sample));

        IDataObject root = JsonDataObject.Parse(stream).GetOrThrow();

        Assert.Equal(1, root.GetInt("id").GetOrThrow());
    }

    [Fact]
    public void Parse_Options_AllowComments()
    {
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        IDataObject root = JsonDataObject.Parse("{ \"id\": 1, // comment\n }", options).GetOrThrow();

        Assert.Equal(1, root.GetInt("id").GetOrThrow());
    }

    [Fact]
    public void Parse_RootUsableAfterDocumentDisposed()
    {
        var root = Parse(Sample);

        GC.Collect();

        Assert.Equal(new[] { 12, 23, 43 }, root.AsArray("tag").GetOrThrow().Select(e => e.GetInt().GetOrThrow()));
    }

    [Fact]
    public void Constructor_UndefinedElement_Throws()
    {
        Assert.Throws<ArgumentException>(() => new JsonDataObject(default));
    }

    [Theory]
    [InlineData("{}", DataKind.Object)]
    [InlineData("[]", DataKind.Array)]
    [InlineData("\"text\"", DataKind.Value)]
    [InlineData("12", DataKind.Value)]
    [InlineData("true", DataKind.Value)]
    [InlineData("false", DataKind.Value)]
    [InlineData("null", DataKind.Value)]
    public void Kind_MatchesJson(string json, DataKind kind)
    {
        Assert.Equal(kind, Parse(json).Kind);
    }

    [Fact]
    public void KeysAndContainsKey_OnObject()
    {
        var root = Parse(Sample);

        Assert.Equal(new[] { "tag", "id" }, root.Keys);
        Assert.True(root.ContainsKey("id"));
        Assert.False(root.ContainsKey("name"));
    }

    [Fact]
    public void KeysAndContainsKey_OnNonObject()
    {
        var array = Parse("[1]");

        Assert.Empty(array.Keys);
        Assert.False(array.ContainsKey("0"));
    }

    [Fact]
    public void NumberGetters_Succeed()
    {
        var root = Parse("""{ "i": 5, "l": 9999999999, "f": 1.5, "d": 2.25, "m": 0.1 }""");

        Assert.Equal(5, root.GetInt("i").GetOrThrow());
        Assert.Equal(9999999999L, root.GetLong("l").GetOrThrow());
        Assert.Equal(1.5f, root.GetFloat("f").GetOrThrow());
        Assert.Equal(2.25, root.GetDouble("d").GetOrThrow());
        Assert.Equal(0.1m, root.GetDecimal("m").GetOrThrow());
    }

    [Fact]
    public void StringAndBoolGetters_Succeed()
    {
        var root = Parse("""{ "s": "hi", "t": true, "f": false }""");

        Assert.Equal("hi", root.GetString("s").GetOrThrow());
        Assert.True(root.GetBool("t").GetOrThrow());
        Assert.False(root.GetBool("f").GetOrThrow());
    }

    [Fact]
    public void Getters_AreStrict()
    {
        var root = Parse("""{ "s": "12", "n": 12, "b": "true", "z": null }""");

        Assert.IsType<InvalidCastException>(root.GetInt("s").Error);
        Assert.IsType<InvalidCastException>(root.GetString("n").Error);
        Assert.IsType<InvalidCastException>(root.GetBool("b").Error);
        Assert.IsType<InvalidCastException>(root.GetString("z").Error);
        Assert.IsType<InvalidCastException>(root.GetInt("z").Error);
        Assert.IsType<InvalidCastException>(root.GetBool("z").Error);
    }

    [Theory]
    [InlineData("1.5")]
    [InlineData("99999999999")]
    public void GetInt_NumberDoesNotFit_FailsWithOverflow(string json)
    {
        var error = Parse(json).GetInt().Error;

        Assert.IsType<OverflowException>(error);
        Assert.Equal($"$: {json} does not fit in Int32", error!.Message);
    }

    [Fact]
    public void GetLong_Fraction_FailsWithOverflow()
    {
        Assert.IsType<OverflowException>(Parse("1.5").GetLong().Error);
    }

    [Fact]
    public void KeyedGetters_ReadSample()
    {
        var root = Parse(Sample);

        Assert.Equal(1, root.GetInt("id").GetOrThrow());
        Assert.Equal(new[] { 12, 23, 43 }, root.AsArray("tag").GetOrThrow().Select(e => e.GetInt().GetOrThrow()));
        Assert.Equal(3, root.GetDataArray("tag").GetOrThrow().Length);
    }

    [Fact]
    public void MissingKey_FailsWithPath()
    {
        var error = Parse(Sample).GetInt("name").Error;

        Assert.IsType<KeyNotFoundException>(error);
        Assert.Equal("$: key 'name' not found", error!.Message);
    }

    [Fact]
    public void KeyedGetter_OnNonObject_Fails()
    {
        var error = Parse(Sample).GetDataObject("id").GetOrThrow().GetInt("x").Error;

        Assert.IsType<InvalidCastException>(error);
        Assert.Equal("$.id: expected Object but was Number", error!.Message);
    }

    [Fact]
    public void WrongKind_ErrorIncludesPath()
    {
        var error = Parse(Sample).GetInt("tag").Error;

        Assert.Equal("$.tag: expected Number but was Array", error!.Message);
    }

    [Fact]
    public void AsArray_OnNonArray_Fails()
    {
        Assert.IsType<InvalidCastException>(Parse(Sample).AsArray("id").Error);
    }

    [Fact]
    public void Paths_NestedObjectsAndArrays()
    {
        var root = Parse("""{ "a": { "b": [1, 2, { "c": true }] } }""");

        var elements = root.GetDataObject("a").GetOrThrow().AsArray("b").GetOrThrow();

        Assert.Equal("$.a.b[2]", PathOf(elements[2]));
        Assert.Equal("$.a.b[2].c", PathOf(elements[2].GetDataObject("c").GetOrThrow()));
    }

    [Fact]
    public void Paths_SpecialKeysUseBracketForm()
    {
        var root = Parse("""{ "odd key": 1, "it's": 2 }""");

        Assert.Equal("$['odd key']", PathOf(root.GetDataObject("odd key").GetOrThrow()));
        Assert.Equal("$['it\\'s']", PathOf(root.GetDataObject("it's").GetOrThrow()));
    }

    [Fact]
    public void JsonDataArray_LengthAndPaths()
    {
        var array = (JsonDataArray)Parse(Sample).GetDataArray("tag").GetOrThrow();

        Assert.Equal(3, array.Length);
        Assert.Equal("$.tag", array.Path);
        Assert.Equal(new[] { "$.tag[0]", "$.tag[1]", "$.tag[2]" }, array.AsArray().Select(PathOf));
    }

    [Fact]
    public void JsonDataArray_NonArray_Throws()
    {
        var element = ((JsonDataObject)Parse(Sample)).Element;

        Assert.Throws<ArgumentException>(() => new JsonDataArray(element));
    }
}
