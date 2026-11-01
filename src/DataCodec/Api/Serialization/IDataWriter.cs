namespace DataCodec.Api.Serialization;

/// <summary>
/// The low-level calls a file format implements to write its syntax.
/// <see cref="SerializationElement.Encode"/> drives these calls; a writer only emits text or bytes.
/// </summary>
/// <remarks>
/// A writer may throw when its format cannot represent a value. The element being written adds its path to the error.
/// </remarks>
public interface IDataWriter
{
    void WriteStartObject(); 

    void WriteEndObject();

    void WriteStartArray();

    void WriteEndArray();

    /// <summary>Writes the key of the next value inside an object.</summary>
    void WriteKey(string key);

    void WriteInt(int value);

    void WriteLong(long value);

    void WriteFloat(float value);

    void WriteDouble(double value);

    void WriteDecimal(decimal value);

    void WriteString(string value);

    void WriteBool(bool value);

    void WriteNull();
}
