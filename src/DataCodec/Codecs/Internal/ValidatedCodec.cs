using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>Rejects values of an inner codec that fail a rule, when decoding and when encoding.</summary>
internal sealed class ValidatedCodec<T>(ICodec<T> inner, Func<T, bool> predicate, Func<T, string> reason) : ICodec<T>
    where T : notnull
{
    public DataResult<T> Decode(IDataObject data) => inner.Decode(data).FlatMap(value =>
    {
        try
        {
            // reason() runs user code (e.g. ToString) too, so it stays inside the try: Decode never throws.
            return predicate(value)
                ? DataResult<T>.Success(value)
                : DataResult<T>.Fail(new DataDecodeException(data.Path, reason(value)));
        }
        catch (Exception error)
        {
            return DataResult<T>.Fail(new DataDecodeException(data.Path, error.Message, error));
        }
    });

    public void Encode(T value, IDataWriter writer)
    {
        bool valid;
        try
        {
            valid = predicate(value);
        }
        catch (Exception error)
        {
            throw new DataEncodeException(DataPath.Root, error.Message, error);
        }

        if (!valid)
        {
            throw new DataEncodeException(DataPath.Root, reason(value));
        }

        inner.Encode(value, writer);
    }
}
