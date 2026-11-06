using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>Converts the values of an inner codec in both directions.</summary>
internal sealed class XmapCodec<A, B>(ICodec<A> inner, Func<A, B> decode, Func<B, A> encode) : ICodec<B>
    where A : notnull where B : notnull
{
    public DataResult<B> Decode(IDataObject data) =>
        inner.Decode(data).FlatMap(value => Xmap.Forward(value, decode, data.Path));

    public void Encode(B value, IDataWriter writer) => inner.Encode(Xmap.Backward(value, encode), writer);
}

/// <summary>Converts the values of an inner map codec in both directions, keeping it a map codec.</summary>
internal sealed class XmapMapCodec<A, B>(IMapCodec<A> inner, Func<A, B> decode, Func<B, A> encode) : IMapCodec<B>
    where A : notnull where B : notnull
{
    public IReadOnlyList<string> Keys => inner.Keys;

    public DataResult<B> Decode(IDataObject data) =>
        inner.Decode(data).FlatMap(value => Xmap.Forward(value, decode, data.Path));

    public DataResult<B> DecodeFields(IDataObject data) =>
        inner.DecodeFields(data).FlatMap(value => Xmap.Forward(value, decode, data.Path));

    public void Encode(B value, IDataWriter writer) => inner.Encode(Xmap.Backward(value, encode), writer);

    public void EncodeFields(B value, IDataWriter writer) => inner.EncodeFields(Xmap.Backward(value, encode), writer);
}

/// <summary>The conversion and error handling shared by the xmap codecs.</summary>
internal static class Xmap
{
    /// <summary>Runs <paramref name="decode"/>; an exception or a null result becomes a failure at <paramref name="path"/>.</summary>
    public static DataResult<B> Forward<A, B>(A value, Func<A, B> decode, string path) where B : notnull
    {
        B result;
        try
        {
            result = decode(value);
        }
        catch (Exception error)
        {
            return DataResult<B>.Fail(new DataDecodeException(path, error.Message, error));
        }

        return result is null
            ? DataResult<B>.Fail(new DataDecodeException(path, "transform returned null"))
            : DataResult<B>.Success(result);
    }

    /// <summary>Runs <paramref name="encode"/>; an exception or a null result throws <see cref="DataEncodeException"/>.</summary>
    public static A Backward<A, B>(B value, Func<B, A> encode) where A : notnull
    {
        A converted;
        try
        {
            converted = encode(value);
        }
        catch (Exception error) when (error is not DataEncodeException)
        {
            throw new DataEncodeException(DataPath.Root, error.Message, error);
        }

        return converted ?? throw new DataEncodeException(DataPath.Root, "transform returned null");
    }
}
