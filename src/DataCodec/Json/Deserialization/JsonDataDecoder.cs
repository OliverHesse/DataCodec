using System.Text.Json;
using DataCodec.Api.Deserialization;

namespace DataCodec.Json.Deserialization;

/// <summary>Decodes JSON into <see cref="JsonDataObject"/>s. Invalid JSON gives a failed result holding the <see cref="JsonException"/>.</summary>
public sealed class JsonDataDecoder : IDataDecoder
{
    private readonly JsonDocumentOptions _options;

    public JsonDataDecoder(JsonDocumentOptions options = default)
    {
        _options = options;
    }

    /// <summary>Accepts standard JSON only.</summary>
    public static JsonDataDecoder Default { get; } = new();

    /// <summary>Also accepts comments and trailing commas.</summary>
    public static JsonDataDecoder Lenient { get; } = new(new JsonDocumentOptions
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    });

    public string Name => "json";

    public DataResult<IDataObject> Decode(string text) => JsonDataObject.Parse(text, _options);

    public DataResult<IDataObject> Decode(Stream stream) => JsonDataObject.Parse(stream, _options);
}
