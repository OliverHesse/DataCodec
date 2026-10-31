namespace DataCodec.Api;

/// <summary>
/// A format-independent view of a piece of data: a collection of key-value pairs, an array, or a single value.
/// </summary>
/// <remarks>
/// Getters without a key read this object's own value. Getters with a key read the value stored under that key.
/// The keyed getters have default implementations that go through <see cref="GetDataObject(string)"/>.
/// </remarks>
public interface IDataObject
{
    DataKind Kind { get; }

    /// <summary>The keys of this object, or empty if it is not a <see cref="DataKind.Object"/>.</summary>
    IEnumerable<string> Keys { get; }

    bool ContainsKey(string key);

    bool IsKind(DataKind kind) => Kind == kind;

    bool IsObject() => IsKind(DataKind.Object);

    bool IsArray() => IsKind(DataKind.Array);

    bool IsValue() => IsKind(DataKind.Value);

    /// <summary>Gets the data object stored under <paramref name="key"/>.</summary>
    DataResult<IDataObject> GetDataObject(string key);

    DataResult<IDataArray> GetDataArray();

    DataResult<IDataArray> GetDataArray(string key) => GetDataObject(key).FlatMap(o => o.GetDataArray());

    DataResult<IDataObject[]> AsArray() => GetDataArray().Map(a => a.AsArray());

    DataResult<IDataObject[]> AsArray(string key) => GetDataArray(key).Map(a => a.AsArray());

    DataResult<int> GetInt();

    DataResult<int> GetInt(string key) => GetDataObject(key).FlatMap(o => o.GetInt());

    DataResult<long> GetLong();

    DataResult<long> GetLong(string key) => GetDataObject(key).FlatMap(o => o.GetLong());

    DataResult<float> GetFloat();

    DataResult<float> GetFloat(string key) => GetDataObject(key).FlatMap(o => o.GetFloat());

    DataResult<double> GetDouble();

    DataResult<double> GetDouble(string key) => GetDataObject(key).FlatMap(o => o.GetDouble());

    DataResult<decimal> GetDecimal();

    DataResult<decimal> GetDecimal(string key) => GetDataObject(key).FlatMap(o => o.GetDecimal());

    DataResult<string> GetString();

    DataResult<string> GetString(string key) => GetDataObject(key).FlatMap(o => o.GetString());

    DataResult<bool> GetBool();

    DataResult<bool> GetBool(string key) => GetDataObject(key).FlatMap(o => o.GetBool());
}
