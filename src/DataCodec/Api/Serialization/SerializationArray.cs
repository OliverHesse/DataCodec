using System.Collections;

namespace DataCodec.Api.Serialization;

/// <summary>An ordered list in a serialization tree. Items can be any mix of objects, arrays and primitives.</summary>
/// <remarks>
/// <see cref="AppendObject"/> and <see cref="AppendArray"/> append a child and return it so it can be filled.
/// The primitive appenders add a value and return nothing.
/// </remarks>
public sealed class SerializationArray : SerializationElement, IEnumerable<SerializationElement>
{
    private readonly List<SerializationElement> _items = [];

    public int Count => _items.Count;

    public SerializationElement this[int index] => _items[index];

    public void RemoveAt(int index) => _items.RemoveAt(index);

    /// <summary>Appends a new object and returns it.</summary>
    public SerializationObject AppendObject() => Add(new SerializationObject());

    /// <summary>Appends a new array and returns it.</summary>
    public SerializationArray AppendArray() => Add(new SerializationArray());

    public void AppendInt(int value) => AppendPrimitive(PrimitiveKind.Int, value);

    public void AppendLong(long value) => AppendPrimitive(PrimitiveKind.Long, value);

    public void AppendFloat(float value) => AppendPrimitive(PrimitiveKind.Float, value);

    public void AppendDouble(double value) => AppendPrimitive(PrimitiveKind.Double, value);

    public void AppendDecimal(decimal value) => AppendPrimitive(PrimitiveKind.Decimal, value);

    public void AppendString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        AppendPrimitive(PrimitiveKind.String, value);
    }

    public void AppendBool(bool value) => AppendPrimitive(PrimitiveKind.Bool, value);

    public void AppendNull() => AppendPrimitive(PrimitiveKind.Null, null);

    public IEnumerator<SerializationElement> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal override void Encode(IDataWriter writer, string path)
    {
        writer.WriteStartArray();
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].Encode(writer, DataPath.Index(path, i));
        }

        writer.WriteEndArray();
    }

    private void AppendPrimitive(PrimitiveKind kind, object? value) => Add(new SerializationPrimitive(kind, value));

    private T Add<T>(T item) where T : SerializationElement
    {
        _items.Add(item);
        return item;
    }
}
