using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>Reads and writes an array of values, decoding every item with one codec.</summary>
internal sealed class ArrayCodec<T>(ICodec<T> item) : ICodec<T[]> where T : notnull
{
    public DataResult<T[]> Decode(IDataObject data) => data.AsArray().FlatMap(elements =>
    {
        var result = new T[elements.Length];
        for (int i = 0; i < elements.Length; i++)
        {
            DataResult<T> decoded = item.Decode(elements[i]);
            if (decoded.IsError)
            {
                return DataResult<T[]>.Fail(decoded.Error);
            }

            result[i] = decoded.Value;
        }

        return DataResult<T[]>.Success(result);
    });

    public void Encode(T[] value, IDataWriter writer)
    {
        writer.WriteStartArray();
        for (int i = 0; i < value.Length; i++)
        {
            string path = DataPath.Index(DataPath.Root, i);
            if (value[i] is null)
            {
                throw new DataEncodeException(path, "item is null");
            }

            try
            {
                item.Encode(value[i], writer);
            }
            catch (DataEncodeException error)
            {
                throw error.Under(path);
            }
        }

        writer.WriteEndArray();
    }
}
