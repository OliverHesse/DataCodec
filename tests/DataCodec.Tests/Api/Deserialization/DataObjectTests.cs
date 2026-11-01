using DataCodec.Api.Deserialization;

namespace DataCodec.Tests.Api.Deserialization;

public class DataObjectTests
{
    // { "tag": [12, 23, 43], "id": 1 }
    private static IDataObject CreateSample() => FakeDataObject.Of(new Dictionary<string, FakeDataObject>
    {
        ["tag"] = FakeDataObject.Of(FakeDataObject.Of(12), FakeDataObject.Of(23), FakeDataObject.Of(43)),
        ["id"] = FakeDataObject.Of(1),
    });

    [Fact]
    public void Kind_Checks()
    {
        IDataObject sample = CreateSample();
        IDataObject value = FakeDataObject.Of(1);
        IDataObject array = FakeDataObject.Of(FakeDataObject.Of(1));

        Assert.True(sample.IsObject());
        Assert.True(sample.IsKind(DataKind.Object));
        Assert.False(sample.IsArray());
        Assert.True(array.IsArray());
        Assert.True(value.IsValue());
        Assert.False(value.IsObject());
    }

    [Fact]
    public void KeyedGetter_ReadsChildValue()
    {
        var sample = CreateSample();

        Assert.Equal(1, sample.GetInt("id").GetOrThrow());
        Assert.True(sample.ContainsKey("tag"));
        Assert.Equal(new[] { "id", "tag" }, sample.Keys.Order());
    }

    [Fact]
    public void KeyedGetter_MissingKey_Fails()
    {
        var result = CreateSample().GetInt("missing");

        Assert.IsType<KeyNotFoundException>(result.Error);
    }

    [Fact]
    public void KeyedGetter_WrongType_Fails()
    {
        var result = CreateSample().GetInt("tag");

        Assert.IsType<InvalidCastException>(result.Error);
    }

    [Fact]
    public void AsArray_WithKey_ReturnsElements()
    {
        var elements = CreateSample().AsArray("tag").GetOrThrow();

        Assert.Equal(new[] { 12, 23, 43 }, elements.Select(e => e.GetInt().GetOrThrow()));
    }

    [Fact]
    public void AsArray_OnArray_ReturnsElements()
    {
        IDataObject array = FakeDataObject.Of(FakeDataObject.Of(1), FakeDataObject.Of(2));

        Assert.Equal(2, array.AsArray().GetOrThrow().Length);
        Assert.Equal(2, array.GetDataArray().GetOrThrow().Length);
    }

    [Fact]
    public void AsArray_OnValue_Fails()
    {
        IDataObject value = FakeDataObject.Of(1);

        Assert.True(value.AsArray().IsError);
        Assert.True(CreateSample().AsArray("id").IsError);
    }

    /// <summary>A minimal in-memory implementation that only provides the members without defaults.</summary>
    private sealed class FakeDataObject : IDataObject, IDataArray
    {
        private readonly Dictionary<string, FakeDataObject>? _entries;
        private readonly FakeDataObject[]? _elements;
        private readonly object? _value;

        private FakeDataObject(DataKind kind, Dictionary<string, FakeDataObject>? entries, FakeDataObject[]? elements, object? value)
        {
            Kind = kind;
            _entries = entries;
            _elements = elements;
            _value = value;
        }

        public static FakeDataObject Of(Dictionary<string, FakeDataObject> entries) => new(DataKind.Object, entries, null, null);

        public static FakeDataObject Of(params FakeDataObject[] elements) => new(DataKind.Array, null, elements, null);

        public static FakeDataObject Of(object value) => new(DataKind.Value, null, null, value);

        public DataKind Kind { get; }

        public string Path => "$";

        public IEnumerable<string> Keys => _entries?.Keys ?? Enumerable.Empty<string>();

        public int Length => _elements?.Length ?? 0;

        public bool ContainsKey(string key) => _entries?.ContainsKey(key) ?? false;

        public DataResult<IDataObject> GetDataObject(string key) =>
            _entries is not null && _entries.TryGetValue(key, out var child)
                ? DataResult<IDataObject>.Success(child)
                : DataResult<IDataObject>.Fail(new KeyNotFoundException(key));

        public DataResult<IDataArray> GetDataArray() =>
            _elements is not null
                ? DataResult<IDataArray>.Success(this)
                : DataResult<IDataArray>.Fail(new InvalidCastException("not an array"));

        public IDataObject[] AsArray() => _elements ?? [];

        public DataResult<int> GetInt() => Get<int>();

        public DataResult<long> GetLong() => Get<long>();

        public DataResult<float> GetFloat() => Get<float>();

        public DataResult<double> GetDouble() => Get<double>();

        public DataResult<decimal> GetDecimal() => Get<decimal>();

        public DataResult<string> GetString() => Get<string>();

        public DataResult<bool> GetBool() => Get<bool>();

        private DataResult<T> Get<T>() where T : notnull =>
            _value is T typed
                ? DataResult<T>.Success(typed)
                : DataResult<T>.Fail(new InvalidCastException($"not a {typeof(T).Name}"));
    }
}
