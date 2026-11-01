using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Codecs.Internal;

namespace DataCodec.Codecs;

/// <summary>
/// One key of an object: the codec for its value and the getter that reads it from <typeparamref name="TOwner"/>.
/// Created with <see cref="CodecExtensions.Field"/> or <see cref="CodecExtensions.Optional"/> and passed to
/// <see cref="Codec.Object{TOwner, T1}"/>.
/// </summary>
public sealed class FieldCodec<TOwner, T> : IFieldCodec<TOwner> where TOwner : notnull where T : notnull
{
    private readonly ICodec<T> _codec;
    private readonly Func<TOwner, T> _getter;

    /// <summary>Creates the value used when the key is missing or null; null for a required field.</summary>
    private readonly Func<T>? _defaultFactory;

    internal FieldCodec(string key, ICodec<T> codec, Func<TOwner, T> getter, Func<T>? defaultFactory)
    {
        Key = key;
        _codec = codec;
        _getter = getter;
        _defaultFactory = defaultFactory;
    }

    public string Key { get; }

    DataResult<object> IFieldCodec<TOwner>.Decode(IDataObject parent)
    {
        if (!parent.ContainsKey(Key))
        {
            return _defaultFactory is not null
                ? CreateDefault(DataPath.Key(parent.Path, Key))
                : DataResult<object>.Fail(parent.GetDataObject(Key).Error ?? new KeyNotFoundException(Key));
        }

        return parent.GetDataObject(Key).FlatMap(child =>
        {
            if (child.IsNull())
            {
                return _defaultFactory is not null
                    ? CreateDefault(child.Path)
                    : DataResult<object>.Fail(new DataDecodeException(child.Path, "value is null"));
            }

            return _codec.Decode(child).Map(value => (object)value);
        });
    }

    private DataResult<object> CreateDefault(string path)
    {
        T value;
        try
        {
            value = _defaultFactory!();
        }
        catch (Exception error)
        {
            return DataResult<object>.Fail(new DataDecodeException(path, error.Message, error));
        }

        return value is null
            ? DataResult<object>.Fail(new DataDecodeException(path, "default factory returned null"))
            : DataResult<object>.Success(value);
    }

    void IFieldCodec<TOwner>.Encode(TOwner owner, IDataWriter writer)
    {
        string path = DataPath.Key(DataPath.Root, Key);

        T value;
        try
        {
            value = _getter(owner);
        }
        catch (Exception error) when (error is not DataEncodeException)
        {
            throw new DataEncodeException(path, error.Message, error);
        }

        if (value is null)
        {
            throw new DataEncodeException(path, "getter returned null");
        }

        writer.WriteKey(Key);
        try
        {
            _codec.Encode(value, writer);
        }
        catch (DataEncodeException error)
        {
            throw error.Under(path);
        }
    }
}
