using System.Diagnostics.CodeAnalysis;

namespace DataCodec.Api.Serialization;

/// <summary>
/// A collection of key-value pairs in a serialization tree. Keys are written out in the order they were added.
/// </summary>
/// <remarks>
/// <see cref="WriteObject"/> and <see cref="WriteArray"/> create a child and return it so it can be filled.
/// The primitive writers return this object so calls can be chained. Writing the same key twice throws.
/// </remarks>
public sealed class SerializationObject : SerializationElement
{
    private readonly OrderedDictionary<string, SerializationElement> _children = new();

    public int Count => _children.Count;

    public IEnumerable<string> Keys => _children.Keys;

    public bool ContainsKey(string key) => _children.ContainsKey(key);

    public bool TryGet(string key, [NotNullWhen(true)] out SerializationElement? element) =>
        _children.TryGetValue(key, out element);

    /// <summary>Removes the element under <paramref name="key"/>, so the key can be written again.</summary>
    public bool Remove(string key) => _children.Remove(key);

    /// <summary>Creates an object at <paramref name="key"/> and returns it.</summary>
    /// <exception cref="ArgumentException"><paramref name="key"/> was already written.</exception>
    public SerializationObject WriteObject(string key) => Add(key, new SerializationObject());

    /// <summary>Creates an array at <paramref name="key"/> and returns it.</summary>
    /// <exception cref="ArgumentException"><paramref name="key"/> was already written.</exception>
    public SerializationArray WriteArray(string key) => Add(key, new SerializationArray());

    public SerializationObject WriteInt(string key, int value) => WritePrimitive(key, PrimitiveKind.Int, value);

    public SerializationObject WriteLong(string key, long value) => WritePrimitive(key, PrimitiveKind.Long, value);

    public SerializationObject WriteFloat(string key, float value) => WritePrimitive(key, PrimitiveKind.Float, value);

    public SerializationObject WriteDouble(string key, double value) => WritePrimitive(key, PrimitiveKind.Double, value);

    public SerializationObject WriteDecimal(string key, decimal value) => WritePrimitive(key, PrimitiveKind.Decimal, value);

    public SerializationObject WriteString(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return WritePrimitive(key, PrimitiveKind.String, value);
    }

    public SerializationObject WriteBool(string key, bool value) => WritePrimitive(key, PrimitiveKind.Bool, value);

    public SerializationObject WriteNull(string key) => WritePrimitive(key, PrimitiveKind.Null, null);

    internal override void Encode(IDataWriter writer, string path)
    {
        writer.WriteStartObject();
        foreach ((string key, SerializationElement child) in _children)
        {
            writer.WriteKey(key);
            child.Encode(writer, DataPath.Key(path, key));
        }

        writer.WriteEndObject();
    }

    private SerializationObject WritePrimitive(string key, PrimitiveKind kind, object? value)
    {
        Add(key, new SerializationPrimitive(kind, value));
        return this;
    }

    private T Add<T>(string key, T child) where T : SerializationElement
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!_children.TryAdd(key, child))
        {
            throw new ArgumentException($"Key '{key}' was already written.", nameof(key));
        }

        return child;
    }
}
