using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Codecs;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;
using DataCodec.Tests.TestSupport;

namespace DataCodec.Tests.Codecs;

public class RecursiveCodecTests
{
    private static readonly IDataEncoder Encoder = JsonDataEncoder.Compact;
    private static readonly IDataDecoder Decoder = JsonDataDecoder.Default;

    private sealed record Node(string Name, Node[] Children);

    private static readonly ICodec<Node> NodeCodec = Codec.Recursive<Node>(self => Codec.Object(
        Codec.String.Field("name", (Node n) => n.Name),
        self.ArrayOf().Optional("children", () => [], (Node n) => n.Children),
        (name, children) => new Node(name, children)));

    private const string TreeJson =
        """{"name":"root","children":[{"name":"a","children":[{"name":"a1","children":[]}]},{"name":"b","children":[]}]}""";

    [Fact]
    public void Tree_RoundTrips()
    {
        var tree = new Node("root", [new Node("a", [new Node("a1", [])]), new Node("b", [])]);

        string text = NodeCodec.EncodeToString(tree, Encoder).GetOrThrow();
        Node back = NodeCodec.Decode(text, Decoder).GetOrThrow();

        Assert.Equal(TreeJson, text);
        Assert.Equal(TreeJson, NodeCodec.EncodeToString(back, Encoder).GetOrThrow());
        Assert.Equal("a1", back.Children[0].Children[0].Name);
    }

    [Fact]
    public void Decode_MissingChildren_UsesDefault()
    {
        Node leaf = NodeCodec.Decode("""{"name":"leaf"}""", Decoder).GetOrThrow();

        Assert.Empty(leaf.Children);
    }

    [Fact]
    public void Decode_DeepError_HasPath()
    {
        DataResult<Node> result = NodeCodec.Decode("""{"name":"a","children":[{"name":1}]}""", Decoder);

        Assert.StartsWith("$.children[0].name:", result.Error!.Message);
    }

    [Fact]
    public void Encode_DeepError_HasPath()
    {
        var tree = new Node("root", [new Node("a", [null!])]);

        var error = Assert.Throws<DataEncodeException>(() => NodeCodec.Encode(tree, new RecordingWriter()));

        Assert.Equal("$.children[0].children[0]", error.Path);
    }

    [Fact]
    public void UsedDuringBuild_Throws()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Codec.Recursive<int>(self =>
        {
            self.Encode(1, new RecordingWriter());
            return Codec.Int;
        }));

        Assert.Contains("before it was built", error.Message);
    }

    [Fact]
    public void BuildReturnsNull_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Codec.Recursive<int>(_ => null!));
    }
}
