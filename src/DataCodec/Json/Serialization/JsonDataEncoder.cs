using System.Text;
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

    public void Encode(SerializationElement root, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(stream);

        using var writer = new Utf8JsonWriter(stream, _options);
        root.Encode(new JsonDataWriter(writer));
        writer.Flush();
    }

    public DataResult<string> EncodeToString(SerializationElement root)
    {
        using var buffer = new MemoryStream();
        try
        {
            Encode(root, buffer);
        }
        catch (DataEncodeException error)
        {
            return DataResult<string>.Fail(error);
        }

        return DataResult<string>.Success(Encoding.UTF8.GetString(buffer.ToArray()));
    }
}
