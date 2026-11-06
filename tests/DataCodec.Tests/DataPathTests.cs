using DataCodec.Api.Serialization;

namespace DataCodec.Tests;

public class DataPathTests
{
    [Fact]
    public void Key_PlainIdentifier() => Assert.Equal("$.name", DataPath.Key(DataPath.Root, "name"));

    [Fact]
    public void Key_OddKey_IsQuoted() => Assert.Equal("$['a b']", DataPath.Key(DataPath.Root, "a b"));

    [Fact]
    public void Index_AppendsBrackets() => Assert.Equal("$.items[2]", DataPath.Index("$.items", 2));

    [Fact]
    public void Rebase_MovesUnderParent() => Assert.Equal("$.stats.speed", DataPath.Rebase("$.speed", "$.stats"));

    [Fact]
    public void Under_MovesEncodeErrorPath()
    {
        var error = new DataEncodeException("$.b", "bad").Under("$.a");

        Assert.Equal("$.a.b", error.Path);
        Assert.Equal("bad", error.Reason);
    }
}
