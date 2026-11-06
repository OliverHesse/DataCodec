using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>
/// Reads an object field by field and builds <typeparamref name="TOwner"/> with a supplier; writes each field in
/// declaration order. The typed <c>Codec.Object</c> overloads wrap their suppliers into the untyped one used here.
/// </summary>
internal sealed class ObjectCodec<TOwner> : IMapCodec<TOwner> where TOwner : notnull
{
    private readonly IReadOnlyList<IFieldCodec<TOwner>> _fields;
    private readonly Func<object[], TOwner> _supplier;
    private readonly string[] _keys;

    /// <exception cref="ArgumentException">Two fields have the same key.</exception>
    public ObjectCodec(IReadOnlyList<IFieldCodec<TOwner>> fields, Func<object[], TOwner> supplier)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (IFieldCodec<TOwner> field in fields)
        {
            ArgumentNullException.ThrowIfNull(field);
            if (!keys.Add(field.Key))
            {
                throw new ArgumentException($"Duplicate field key '{field.Key}'.", nameof(fields));
            }
        }

        _fields = fields;
        _supplier = supplier;
        _keys = fields.Select(field => field.Key).ToArray();
    }

    public IReadOnlyList<string> Keys => _keys;

    public DataResult<TOwner> Decode(IDataObject data)
    {
        if (!data.IsObject())
        {
            return DataResult<TOwner>.Fail(new DataDecodeException(data.Path, $"expected Object but was {data.Kind}"));
        }

        return DecodeFields(data);
    }

    public DataResult<TOwner> DecodeFields(IDataObject data)
    {
        var values = new object[_fields.Count];
        for (int i = 0; i < _fields.Count; i++)
        {
            DataResult<object> decoded = _fields[i].Decode(data);
            if (decoded.IsError)
            {
                return DataResult<TOwner>.Fail(decoded.Error);
            }

            values[i] = decoded.Value;
        }

        TOwner result;
        try
        {
            result = _supplier(values);
        }
        catch (Exception error)
        {
            // A constructor rejecting the values means the data broke the type's rules.
            return DataResult<TOwner>.Fail(new DataDecodeException(data.Path, error.Message, error));
        }

        return result is null
            ? DataResult<TOwner>.Fail(new DataDecodeException(data.Path, "supplier returned null"))
            : DataResult<TOwner>.Success(result);
    }

    public void Encode(TOwner value, IDataWriter writer)
    {
        writer.WriteStartObject();
        EncodeFields(value, writer);
        writer.WriteEndObject();
    }

    public void EncodeFields(TOwner value, IDataWriter writer)
    {
        foreach (IFieldCodec<TOwner> field in _fields)
        {
            field.Encode(value, writer);
        }
    }
}
