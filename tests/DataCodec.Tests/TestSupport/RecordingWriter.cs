using System.Globalization;
using DataCodec.Api.Serialization;

namespace DataCodec.Tests.TestSupport;

/// <summary>An <see cref="IDataWriter"/> that records each call as text, e.g. <c>"key:id"</c> or <c>"int:1"</c>.</summary>
internal sealed class RecordingWriter : IDataWriter
{
    public List<string> Calls { get; } = [];

    /// <summary>When set, <see cref="WriteDouble"/> throws <see cref="NotSupportedException"/>.</summary>
    public bool FailDoubles { get; init; }

    public void WriteStartObject() => Calls.Add("{");

    public void WriteEndObject() => Calls.Add("}");

    public void WriteStartArray() => Calls.Add("[");

    public void WriteEndArray() => Calls.Add("]");

    public void WriteKey(string key) => Calls.Add($"key:{key}");

    public void WriteInt(int value) => Calls.Add($"int:{value}");

    public void WriteLong(long value) => Calls.Add($"long:{value}");

    public void WriteFloat(float value) => Calls.Add($"float:{value.ToString(CultureInfo.InvariantCulture)}");

    public void WriteDouble(double value)
    {
        if (FailDoubles)
        {
            throw new NotSupportedException("doubles not supported");
        }

        Calls.Add($"double:{value.ToString(CultureInfo.InvariantCulture)}");
    }

    public void WriteDecimal(decimal value) => Calls.Add($"decimal:{value.ToString(CultureInfo.InvariantCulture)}");

    public void WriteString(string value) => Calls.Add($"string:{value}");

    public void WriteBool(bool value) => Calls.Add($"bool:{value}");

    public void WriteNull() => Calls.Add("null");
}
