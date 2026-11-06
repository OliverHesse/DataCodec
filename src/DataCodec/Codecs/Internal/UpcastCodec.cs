using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>Uses a codec for <typeparamref name="TSub"/> as a codec for its base type <typeparamref name="TBase"/>.</summary>
internal sealed class UpcastCodec<TSub, TBase>(ICodec<TSub> inner) : ICodec<TBase>
    where TSub : notnull, TBase where TBase : notnull
{
    public DataResult<TBase> Decode(IDataObject data) => inner.Decode(data).Map(value => (TBase)value);

    public void Encode(TBase value, IDataWriter writer) => inner.Encode(Upcast.Narrow<TSub, TBase>(value), writer);
}

/// <summary>Uses a map codec for <typeparamref name="TSub"/> as a map codec for its base type <typeparamref name="TBase"/>.</summary>
internal sealed class UpcastMapCodec<TSub, TBase>(IMapCodec<TSub> inner) : IMapCodec<TBase>
    where TSub : notnull, TBase where TBase : notnull
{
    public IReadOnlyList<string> Keys => inner.Keys;

    public DataResult<TBase> Decode(IDataObject data) => inner.Decode(data).Map(value => (TBase)value);

    public DataResult<TBase> DecodeFields(IDataObject data) => inner.DecodeFields(data).Map(value => (TBase)value);

    public void Encode(TBase value, IDataWriter writer) => inner.Encode(Upcast.Narrow<TSub, TBase>(value), writer);

    public void EncodeFields(TBase value, IDataWriter writer) =>
        inner.EncodeFields(Upcast.Narrow<TSub, TBase>(value), writer);
}

/// <summary>Shared checked cast for the upcast codecs.</summary>
internal static class Upcast
{
    /// <exception cref="DataEncodeException"><paramref name="value"/> is not a <typeparamref name="TSub"/>.</exception>
    public static TSub Narrow<TSub, TBase>(TBase value) where TSub : notnull, TBase where TBase : notnull =>
        value is TSub sub
            ? sub
            : throw new DataEncodeException(
                DataPath.Root,
                $"expected {typeof(TSub).Name} but was {value?.GetType().Name ?? "null"}");
}
