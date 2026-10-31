using System.Text.Json;
using DataCodec.Api.Serialization;

namespace DataCodec.Json.Serialization;

/// <summary>An <see cref="IDataWriter"/> that writes JSON through a <see cref="Utf8JsonWriter"/>.</summary>
internal sealed class JsonDataWriter(Utf8JsonWriter writer) : IDataWriter
{
    public void WriteStartObject() => writer.WriteStartObject();

    public void WriteEndObject() => writer.WriteEndObject();

    public void WriteStartArray() => writer.WriteStartArray();

    public void WriteEndArray() => writer.WriteEndArray();

    public void WriteKey(string key) => writer.WritePropertyName(key);

    public void WriteInt(int value) => writer.WriteNumberValue(value);

    public void WriteLong(long value) => writer.WriteNumberValue(value);

    public void WriteFloat(float value)
    {
        EnsureFinite(float.IsFinite(value), value);
        writer.WriteNumberValue(value);
    }

    public void WriteDouble(double value)
    {
        EnsureFinite(double.IsFinite(value), value);
        writer.WriteNumberValue(value);
    }

    public void WriteDecimal(decimal value) => writer.WriteNumberValue(value);

    public void WriteString(string value) => writer.WriteStringValue(value);

    public void WriteBool(bool value) => writer.WriteBooleanValue(value);

    public void WriteNull() => writer.WriteNullValue();

    private static void EnsureFinite(bool isFinite, object value)
    {
        if (!isFinite)
        {
            throw new ArgumentException($"{value} cannot be written as JSON");
        }
    }
}
