using dev.oliverhesse.core;

namespace CoreLibrary.Tests.dev.oliverhesse.core;

public class RegistryTests
{
    private static readonly QualifiedIdentifier RegistryId = QualifiedIdentifier.Parse("core:blocks");
    private static readonly QualifiedIdentifier Stone = QualifiedIdentifier.Parse("game:stone");
    private static readonly QualifiedIdentifier Dirt = QualifiedIdentifier.Parse("game:dirt");

    private static DynamicRegistry<string> CreateRegistry()
    {
        var registry = new DynamicRegistry<string>(RegistryId);
        registry.Add(Stone, "stone-block");
        registry.Add(Dirt, "dirt-block");
        return registry;
    }

    [Fact]
    public void Constructor_SetsId()
    {
        Assert.Equal(RegistryId, new DynamicRegistry<string>(RegistryId).Id);
    }

    [Fact]
    public void Constructor_NullId_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DynamicRegistry<string>(null!));
    }

    [Fact]
    public void Add_ThenGet_ReturnsValue()
    {
        var registry = CreateRegistry();

        Assert.Equal("stone-block", registry[Stone]);
        Assert.True(registry.ContainsKey(Stone));
        Assert.True(registry.TryGetValue(Dirt, out var dirt));
        Assert.Equal("dirt-block", dirt);
        Assert.False(registry.IsReadOnly);
    }

    [Fact]
    public void Add_DuplicateKey_Throws()
    {
        var registry = CreateRegistry();

        Assert.Throws<ArgumentException>(() => registry.Add(Stone, "other"));
    }

    [Fact]
    public void Add_DuplicateValue_Throws()
    {
        var registry = CreateRegistry();

        Assert.Throws<ArgumentException>(() => registry.Add(QualifiedIdentifier.Parse("game:rock"), "stone-block"));
    }

    [Fact]
    public void Add_Null_Throws()
    {
        var registry = new DynamicRegistry<string>(RegistryId);

        Assert.Throws<ArgumentNullException>(() => registry.Add(null!, "value"));
        Assert.Throws<ArgumentNullException>(() => registry.Add(Stone, null!));
    }

    [Fact]
    public void TryAdd_ReturnsFalseForExistingKey()
    {
        var registry = CreateRegistry();

        Assert.False(registry.TryAdd(Stone, "other"));
        Assert.Equal("stone-block", registry[Stone]);
        Assert.True(registry.TryAdd(QualifiedIdentifier.Parse("game:sand"), "sand-block"));
        Assert.Equal(3, registry.Count);
    }

    [Fact]
    public void Get_MissingKey_ThrowsKeyNotFound()
    {
        var registry = CreateRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry[QualifiedIdentifier.Parse("game:missing")]);
        Assert.False(registry.TryGetValue(QualifiedIdentifier.Parse("game:missing"), out _));
    }

    [Fact]
    public void IndexerSet_AddsAndReplaces()
    {
        var registry = CreateRegistry();
        var sand = QualifiedIdentifier.Parse("game:sand");

        registry[sand] = "sand-block";
        registry[Stone] = "smooth-stone";

        Assert.Equal("sand-block", registry[sand]);
        Assert.Equal("smooth-stone", registry[Stone]);
        Assert.False(registry.ContainsValue("stone-block"));
        Assert.Equal(Stone, registry.GetKey("smooth-stone"));
    }

    [Fact]
    public void IndexerSet_ValueUsedByOtherKey_Throws()
    {
        var registry = CreateRegistry();

        Assert.Throws<ArgumentException>(() => registry[Stone] = "dirt-block");
    }

    [Fact]
    public void IndexerSet_SameValueSameKey_IsAllowed()
    {
        var registry = CreateRegistry();

        registry[Stone] = "stone-block";

        Assert.Equal(Stone, registry.GetKey("stone-block"));
    }

    [Fact]
    public void Remove_RemovesKeyAndValue()
    {
        var registry = CreateRegistry();

        Assert.True(registry.Remove(Stone));
        Assert.False(registry.Remove(Stone));
        Assert.False(registry.ContainsKey(Stone));
        Assert.False(registry.ContainsValue("stone-block"));
        Assert.Single(registry);
    }

    [Fact]
    public void Clear_RemovesEverything()
    {
        var registry = CreateRegistry();

        registry.Clear();

        Assert.Empty(registry);
        Assert.False(registry.ContainsValue("stone-block"));
    }

    [Fact]
    public void KeysValuesAndEnumeration_ReflectContents()
    {
        var registry = CreateRegistry();

        Assert.Equal(2, registry.Count);
        Assert.Equal(new[] { Dirt, Stone }.OrderBy(k => k.ToString()), registry.Keys.OrderBy(k => k.ToString()));
        Assert.Equal(new[] { "dirt-block", "stone-block" }, registry.Values.Order());
        Assert.Contains(new KeyValuePair<QualifiedIdentifier, string>(Stone, "stone-block"), registry);
    }

    [Fact]
    public void ReverseLookup_FindsKey()
    {
        var registry = CreateRegistry();

        Assert.Equal(Stone, registry.GetKey("stone-block"));
        Assert.True(registry.TryGetKey("dirt-block", out var key));
        Assert.Equal(Dirt, key);
        Assert.True(registry.ContainsValue("dirt-block"));
        Assert.False(registry.TryGetKey("missing", out _));
        Assert.Throws<KeyNotFoundException>(() => registry.GetKey("missing"));
    }

    [Fact]
    public void Freeze_CopiesContentsAndId()
    {
        var registry = CreateRegistry();

        var frozen = registry.Freeze();

        Assert.Equal(RegistryId, frozen.Id);
        Assert.True(frozen.IsReadOnly);
        Assert.Equal(2, frozen.Count);
        Assert.Equal("stone-block", frozen[Stone]);
        Assert.Equal(Dirt, frozen.GetKey("dirt-block"));
    }

    [Fact]
    public void Freeze_IsSnapshot()
    {
        var registry = CreateRegistry();
        var frozen = registry.Freeze();

        registry.Remove(Stone);
        registry.Add(QualifiedIdentifier.Parse("game:sand"), "sand-block");

        Assert.Equal(2, frozen.Count);
        Assert.True(frozen.ContainsKey(Stone));
        Assert.False(frozen.ContainsValue("sand-block"));
    }

    [Fact]
    public void Freeze_OnFrozen_ReturnsSameInstance()
    {
        var frozen = CreateRegistry().Freeze();

        Assert.Same(frozen, frozen.Freeze());
    }

    [Fact]
    public void Frozen_ModifyMethods_Throw()
    {
        var frozen = CreateRegistry().Freeze();
        var sand = QualifiedIdentifier.Parse("game:sand");

        Assert.Throws<NotSupportedException>(() => frozen.Add(sand, "sand-block"));
        Assert.Throws<NotSupportedException>(() => frozen.TryAdd(sand, "sand-block"));
        Assert.Throws<NotSupportedException>(() => frozen[sand] = "sand-block");
        Assert.Throws<NotSupportedException>(() => frozen.Remove(Stone));
        Assert.Throws<NotSupportedException>(() => frozen.Clear());
        Assert.Equal(2, frozen.Count);
    }

    [Fact]
    public void Frozen_ModifyThroughInterface_Throws()
    {
        IDictionary<QualifiedIdentifier, string> frozen = CreateRegistry().Freeze();
        var sand = QualifiedIdentifier.Parse("game:sand");
        var missingPair = new KeyValuePair<QualifiedIdentifier, string>(sand, "sand-block");

        Assert.Throws<NotSupportedException>(() => frozen.Add(missingPair));
        Assert.Throws<NotSupportedException>(() => frozen.Remove(missingPair));
        Assert.Throws<NotSupportedException>(() => frozen.Keys.Add(sand));
    }

    [Fact]
    public void Registry_WorksAsDictionaryInterface()
    {
        IDictionary<QualifiedIdentifier, string> registry = CreateRegistry();
        var pair = new KeyValuePair<QualifiedIdentifier, string>(Stone, "stone-block");

        Assert.Contains(pair, registry);
        Assert.True(registry.Remove(pair));
        Assert.False(registry.Remove(pair));

        var array = new KeyValuePair<QualifiedIdentifier, string>[1];
        registry.CopyTo(array, 0);
        Assert.Equal(Dirt, array[0].Key);
    }
}
