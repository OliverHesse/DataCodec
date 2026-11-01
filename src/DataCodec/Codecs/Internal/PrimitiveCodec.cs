using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>A codec that maps one reader getter to one writer method.</summary>
internal sealed class PrimitiveCodec<T>(Func<IDataObject, DataResult<T>> decode, Action<IDataWriter, T> encode)
    : ICodec<T> where T : notnull
{
    public DataResult<T> Decode(IDataObject data) => decode(data);

    public void Encode(T value, IDataWriter writer)
    {
        try
        {
            encode(writer, value);
        }
        catch (Exception error) when (error is not DataEncodeException)
        {
            // Writers do not track paths; the enclosing codecs rebase this onto the full path.
            throw new DataEncodeException(DataPath.Root, error.Message, error);
        }
    }
}
