using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>Reads and writes a list from an array, decoding every item with one codec.</summary>
internal sealed class ListCodec<T>(ICodec<T> item) : ICodec<List<T>> where T : notnull
{
    public DataResult<List<T>> Decode(IDataObject data) => data.AsArray().FlatMap(elements =>
    {
        var result = new List<T>(elements.Length);
        foreach (IDataObject element in elements)
        {
            DataResult<T> decoded = item.Decode(element);
            if (decoded.IsError)
            {
                return DataResult<List<T>>.Fail(decoded.Error);
            }

            result.Add(decoded.Value);
        }

        return DataResult<List<T>>.Success(result);
    });

    public void Encode(List<T> value, IDataWriter writer)
    {
        writer.WriteStartArray();
        for (int i = 0; i < value.Count; i++)
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
