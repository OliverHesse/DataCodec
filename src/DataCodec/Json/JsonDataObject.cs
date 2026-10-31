using System.Text.Json;
using DataCodec.Api;

namespace DataCodec.Json;

/// <summary>
/// An <see cref="IDataObject"/> backed by a <see cref="JsonElement"/>. Conversions are strict: a JSON string
/// <c>"12"</c> is not an int. Error messages start with the element's JSON path, e.g. <c>$.tag[1]</c>.
/// </summary>
public sealed class JsonDataObject : IDataObject
{
    private delegate bool TryRead<T>(JsonElement element, out T value);

    public JsonElement Element { get; }

    /// <summary>Where this element sits in the document, e.g. <c>$</c>, <c>$.tag</c> or <c>$.tag[1]</c>.</summary>
    public string Path { get; }

    /// <exception cref="ArgumentException"><paramref name="element"/> is a default (undefined) element.</exception>
    public JsonDataObject(JsonElement element, string path = "$")
    {
        ArgumentNullException.ThrowIfNull(path);
        if (element.ValueKind == JsonValueKind.Undefined)
        {
            throw new ArgumentException("The JSON element is undefined.", nameof(element));
        }

        Element = element;
        Path = path;
    }

    /// <summary>Parses a JSON document. Invalid JSON gives a failed result holding the <see cref="JsonException"/>.</summary>
    public static DataResult<JsonDataObject> Parse(string json, JsonDocumentOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(json);
        return ParseDocument(() => JsonDocument.Parse(json, options));
    }

    /// <summary>Parses a UTF-8 JSON document. Invalid JSON gives a failed result holding the <see cref="JsonException"/>.</summary>
    public static DataResult<JsonDataObject> Parse(Stream utf8Json, JsonDocumentOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Json);
        return ParseDocument(() => JsonDocument.Parse(utf8Json, options));
    }

    public DataKind Kind => Element.ValueKind switch
    {
        JsonValueKind.Object => DataKind.Object,
        JsonValueKind.Array => DataKind.Array,
        _ => DataKind.Value,
    };

    public IEnumerable<string> Keys =>
        Element.ValueKind == JsonValueKind.Object
            ? Element.EnumerateObject().Select(property => property.Name)
            : [];

    public bool ContainsKey(string key) =>
        Element.ValueKind == JsonValueKind.Object && Element.TryGetProperty(key, out _);

    public DataResult<IDataObject> GetDataObject(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (Element.ValueKind != JsonValueKind.Object)
        {
            return DataResult<IDataObject>.Fail(WrongKind("Object"));
        }

        if (!Element.TryGetProperty(key, out JsonElement child))
        {
            return DataResult<IDataObject>.Fail(new KeyNotFoundException($"{Path}: key '{key}' not found"));
        }

        return DataResult<IDataObject>.Success(new JsonDataObject(child, ChildPath(key)));
    }

    public DataResult<IDataArray> GetDataArray() =>
        Element.ValueKind == JsonValueKind.Array
            ? DataResult<IDataArray>.Success(new JsonDataArray(Element, Path))
            : DataResult<IDataArray>.Fail(WrongKind("Array"));

    public DataResult<int> GetInt() => GetNumber((JsonElement e, out int v) => e.TryGetInt32(out v), "Int32");

    public DataResult<long> GetLong() => GetNumber((JsonElement e, out long v) => e.TryGetInt64(out v), "Int64");

    public DataResult<float> GetFloat() => GetNumber((JsonElement e, out float v) => e.TryGetSingle(out v), "Single");

    public DataResult<double> GetDouble() => GetNumber((JsonElement e, out double v) => e.TryGetDouble(out v), "Double");

    public DataResult<decimal> GetDecimal() => GetNumber((JsonElement e, out decimal v) => e.TryGetDecimal(out v), "Decimal");

    public DataResult<string> GetString() =>
        Element.ValueKind == JsonValueKind.String
            ? DataResult<string>.Success(Element.GetString()!)
            : DataResult<string>.Fail(WrongKind("String"));

    public DataResult<bool> GetBool() => Element.ValueKind switch
    {
        JsonValueKind.True => DataResult<bool>.Success(true),
        JsonValueKind.False => DataResult<bool>.Success(false),
        _ => DataResult<bool>.Fail(WrongKind("Boolean")),
    };

    public override string ToString() => Element.GetRawText();

    private static DataResult<JsonDataObject> ParseDocument(Func<JsonDocument> parse)
    {
        try
        {
            using JsonDocument document = parse();
            // Clone so the element stays valid after the document is disposed.
            return DataResult<JsonDataObject>.Success(new JsonDataObject(document.RootElement.Clone()));
        }
        catch (JsonException error)
        {
            return DataResult<JsonDataObject>.Fail(error);
        }
    }

    private DataResult<T> GetNumber<T>(TryRead<T> tryRead, string typeName) where T : notnull
    {
        if (Element.ValueKind != JsonValueKind.Number)
        {
            return DataResult<T>.Fail(WrongKind("Number"));
        }

        return tryRead(Element, out T value)
            ? DataResult<T>.Success(value)
            : DataResult<T>.Fail(new OverflowException($"{Path}: {Element.GetRawText()} does not fit in {typeName}"));
    }

    private InvalidCastException WrongKind(string expected) =>
        new($"{Path}: expected {expected} but was {Element.ValueKind}");

    /// <summary>Builds the path of a property: <c>$.name</c>, or <c>$['odd key']</c> if the key is not a plain identifier.</summary>
    private string ChildPath(string key)
    {
        bool isIdentifier = key.Length > 0
            && (char.IsLetter(key[0]) || key[0] == '_')
            && key.All(c => char.IsLetterOrDigit(c) || c == '_');

        return isIdentifier ? $"{Path}.{key}" : $"{Path}['{key.Replace("'", "\\'")}']";
    }
}
