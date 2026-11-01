using System.Text.Json;
using DataCodec.Api.Serialization;

namespace DataCodec.Json.Serialization;

/// <summary>Encodes a serialization tree as JSON.</summary>
public sealed class JsonDataEncoder : IDataEncoder
{
    private readonly JsonWriterOptions _options;

    public JsonDataEncoder(JsonWriterOptions options = default)
    {
        _options = options;
    }

    /// <summary>Writes JSON on a single line with no extra whitespace.</summary>
    public static JsonDataEncoder Compact { get; } = new();

    /// <summary>Writes JSON with one value per line, indented by two spaces.</summary>
    public static JsonDataEncoder Indented { get; } = new(new JsonWriterOptions { Indented = true });

    public string Name => "json";

    public void Encode(Stream stream, Action<IDataWriter> write)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(write);

        using var writer = new Utf8JsonWriter(stream, _options);
        write(new JsonDataWriter(writer));
        writer.Flush();
    }
}
