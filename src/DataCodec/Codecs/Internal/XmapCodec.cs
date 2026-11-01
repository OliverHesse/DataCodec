using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>Converts the values of an inner codec in both directions.</summary>
internal sealed class XmapCodec<A, B>(ICodec<A> inner, Func<A, B> decode, Func<B, A> encode) : ICodec<B>
    where A : notnull where B : notnull
{
    public DataResult<B> Decode(IDataObject data) => inner.Decode(data).FlatMap(value =>
    {
        B result;
        try
        {
            result = decode(value);
        }
        catch (Exception error)
        {
            return DataResult<B>.Fail(new DataDecodeException(data.Path, error.Message, error));
        }

        return result is null
            ? DataResult<B>.Fail(new DataDecodeException(data.Path, "transform returned null"))
            : DataResult<B>.Success(result);
    });

    public void Encode(B value, IDataWriter writer)
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

        if (converted is null)
        {
            throw new DataEncodeException(DataPath.Root, "transform returned null");
        }

        inner.Encode(converted, writer);
    }
}
