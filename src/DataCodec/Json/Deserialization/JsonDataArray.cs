using System.Text.Json;
using DataCodec.Api.Deserialization;

namespace DataCodec.Json.Deserialization;

/// <summary>An <see cref="IDataArray"/> backed by a JSON array <see cref="JsonElement"/>.</summary>
public sealed class JsonDataArray : IDataArray
{
    public JsonElement Element { get; }

    /// <summary>Where this array sits in the document, e.g. <c>$.tag</c>.</summary>
    public string Path { get; }

    /// <exception cref="ArgumentException"><paramref name="element"/> is not a JSON array.</exception>
    public JsonDataArray(JsonElement element, string path = "$")
    {
        ArgumentNullException.ThrowIfNull(path);
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException($"{path}: expected Array but was {element.ValueKind}", nameof(element));
        }

        Element = element;
        Path = path;
    }

    public int Length => Element.GetArrayLength();

    public IDataObject[] AsArray() =>
        Element.EnumerateArray()
            .Select((element, index) => (IDataObject)new JsonDataObject(element, DataPath.Index(Path, index)))
            .ToArray();

    public override string ToString() => Element.GetRawText();
}
