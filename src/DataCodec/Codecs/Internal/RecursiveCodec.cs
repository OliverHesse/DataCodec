using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>A codec that is passed to its own build function, so it can contain itself. Created by <see cref="Codec.Recursive{T}"/>.</summary>
internal sealed class RecursiveCodec<T> : ICodec<T> where T : notnull
{
    private readonly ICodec<T>? _inner;

    public RecursiveCodec(Func<ICodec<T>, ICodec<T>> build)
    {
        _inner = build(this) ?? throw new InvalidOperationException("recursive codec build function returned null");
    }

    public DataResult<T> Decode(IDataObject data) => Inner.Decode(data);

    public void Encode(T value, IDataWriter writer) => Inner.Encode(value, writer);

    private ICodec<T> Inner =>
        _inner ?? throw new InvalidOperationException(
            "recursive codec used before it was built; only store the self reference inside the build function");
}
