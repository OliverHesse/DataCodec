namespace dev.oliverhesse.core;

/// <summary>A registry that can be modified. Use <see cref="Registry{T}.Freeze"/> to take a read-only snapshot.</summary>
public sealed class DynamicRegistry<T> : Registry<T> where T : notnull
{
    private readonly Dictionary<QualifiedIdentifier, T> _entries = new();
    private readonly Dictionary<T, QualifiedIdentifier> _reverseEntries = new();

    public DynamicRegistry(QualifiedIdentifier id) : base(id)
    {
    }

    protected override IDictionary<QualifiedIdentifier, T> Entries => _entries;

    protected override IReadOnlyDictionary<T, QualifiedIdentifier> ReverseEntries => _reverseEntries;

    public override bool IsReadOnly => false;

    public override void Add(QualifiedIdentifier key, T value)
    {
        if (!TryAdd(key, value))
        {
            throw new ArgumentException($"'{key}' is already registered in '{Id}'.", nameof(key));
        }
    }

    public override bool TryAdd(QualifiedIdentifier key, T value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        if (_entries.ContainsKey(key))
        {
            return false;
        }

        EnsureValueFree(key, value);
        _entries.Add(key, value);
        _reverseEntries.Add(value, key);
        return true;
    }

    public override bool Remove(QualifiedIdentifier key)
    {
        if (!_entries.Remove(key, out T? value))
        {
            return false;
        }

        _reverseEntries.Remove(value);
        return true;
    }

    public override void Clear()
    {
        _entries.Clear();
        _reverseEntries.Clear();
    }

    protected override void Set(QualifiedIdentifier key, T value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        EnsureValueFree(key, value);
        if (_entries.TryGetValue(key, out T? old))
        {
            _reverseEntries.Remove(old);
        }

        _entries[key] = value;
        _reverseEntries[value] = key;
    }

    /// <summary>Throws if <paramref name="value"/> is already registered under a key other than <paramref name="key"/>.</summary>
    private void EnsureValueFree(QualifiedIdentifier key, T value)
    {
        if (_reverseEntries.TryGetValue(value, out QualifiedIdentifier? existing) && existing != key)
        {
            throw new ArgumentException($"Value '{value}' is already registered in '{Id}' as '{existing}'.", nameof(value));
        }
    }
}
