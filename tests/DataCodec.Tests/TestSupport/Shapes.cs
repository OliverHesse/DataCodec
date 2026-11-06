using DataCodec.Codecs;

namespace DataCodec.Tests.TestSupport;

/// <summary>A small type hierarchy for tests of subtype codecs.</summary>
internal interface IShape;

internal sealed record Circle(double R) : IShape
{
    public static readonly IMapCodec<Circle> Codec = DataCodec.Codecs.Codec.Object(
        DataCodec.Codecs.Codec.Double.Field("r", (Circle c) => c.R),
        r => new Circle(r));
}

internal sealed record Square(double S) : IShape
{
    public static readonly IMapCodec<Square> Codec = DataCodec.Codecs.Codec.Object(
        DataCodec.Codecs.Codec.Double.Field("s", (Square s) => s.S),
        s => new Square(s));
}
