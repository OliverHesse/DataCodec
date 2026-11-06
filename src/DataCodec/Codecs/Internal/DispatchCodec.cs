using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>
/// Reads and writes <typeparamref name="TBase"/> as one flat object: a type key naming the type, then that type's own
/// fields. Created by <see cref="Codec.Dispatch{TBase, TKey}"/>.
/// </summary>
internal sealed class DispatchCodec<TBase, TKey>(
    ICodec<TKey> keyCodec,
    Func<TBase, TKey> keyOf,
    Func<TKey, IMapCodec<TBase>?> lookup,
    string typeKey) : ICodec<TBase>
    where TBase : notnull where TKey : notnull
{
    public DataResult<TBase> Decode(IDataObject data)
    {
        if (!data.IsObject())
        {
            return DataResult<TBase>.Fail(data.Path, $"expected Object but was {data.Kind}");
        }

        if (!data.ContainsKey(typeKey))
        {
            return DataResult<TBase>.Fail(data.Path, $"missing '{typeKey}' key");
        }

        string typePath = DataPath.Key(data.Path, typeKey);
        return data.GetDataObject(typeKey)
            .FlatMap(keyCodec.Decode)
            .FlatMap(key =>
            {
                IMapCodec<TBase>? codec = lookup(key);
                if (codec is null)
                {
                    return DataResult<TBase>.Fail(typePath, $"unknown type '{key}'");
                }

                return HasOwnTypeKey(codec)
                    ? DataResult<TBase>.Fail(typePath, CollisionReason(key))
                    : codec.DecodeFields(data);
            });
    }

    public void Encode(TBase value, IDataWriter writer)
    {
        TKey key;
        try
        {
            key = keyOf(value);
        }
        catch (Exception error) when (error is not DataEncodeException)
        {
            throw new DataEncodeException(DataPath.Root, error.Message, error);
        }

        string typePath = DataPath.Key(DataPath.Root, typeKey);
        if (key is null)
        {
            throw new DataEncodeException(typePath, "type key is null");
        }

        IMapCodec<TBase> codec = lookup(key) ?? throw new DataEncodeException(typePath, $"no codec for type '{key}'");
        if (HasOwnTypeKey(codec))
        {
            throw new DataEncodeException(typePath, CollisionReason(key));
        }

        writer.WriteStartObject();
        writer.WriteKey(typeKey);
        try
        {
            keyCodec.Encode(key, writer);
        }
        catch (DataEncodeException error)
        {
            throw error.Under(typePath);
        }

        codec.EncodeFields(value, writer);
        writer.WriteEndObject();
    }

    private bool HasOwnTypeKey(IMapCodec<TBase> codec) => codec.Keys.Contains(typeKey, StringComparer.Ordinal);

    private string CollisionReason(TKey key) => $"type '{key}' has its own '{typeKey}' field, which clashes with the type key";
}
