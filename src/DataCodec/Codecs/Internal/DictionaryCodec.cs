using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>Reads and writes a string-keyed dictionary from an object, decoding every value with one codec.</summary>
internal sealed class DictionaryCodec<T>(ICodec<T> value) : ICodec<Dictionary<string, T>> where T : notnull
{
    public DataResult<Dictionary<string, T>> Decode(IDataObject data)
    {
        if (!data.IsObject())
        {
            return DataResult<Dictionary<string, T>>.Fail(
                new DataDecodeException(data.Path, $"expected Object but was {data.Kind}"));
        }

        var result = new Dictionary<string, T>();
        foreach (string key in data.Keys)
        {
            DataResult<T> decoded = data.GetDataObject(key).FlatMap(value.Decode);
            if (decoded.IsError)
            {
                return DataResult<Dictionary<string, T>>.Fail(decoded.Error);
            }

            // The indexer, not Add: if the data repeats a key, the last value wins instead of throwing.
            result[key] = decoded.Value;
        }

        return DataResult<Dictionary<string, T>>.Success(result);
    }

    public void Encode(Dictionary<string, T> entries, IDataWriter writer)
    {
        writer.WriteStartObject();
        foreach ((string key, T entry) in entries)
        {
            string path = DataPath.Key(DataPath.Root, key);
            if (entry is null)
            {
                throw new DataEncodeException(path, "value is null");
            }

            writer.WriteKey(key);
            try
            {
                value.Encode(entry, writer);
            }
            catch (DataEncodeException error)
            {
                throw error.Under(path);
            }
        }

        writer.WriteEndObject();
    }
}
