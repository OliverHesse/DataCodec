using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace dev.oliverhesse.core;

/// <summary>
/// A named map of <see cref="QualifiedIdentifier"/> keys to unique, non-null values, with reverse lookup.
/// </summary>
/// <remarks>
/// Use <see cref="DynamicRegistry{T}"/> to build a registry and <see cref="Freeze"/> to take a read-only snapshot.
/// Modify methods on a frozen registry throw <see cref="NotSupportedException"/>.
/// </remarks>
public abstract class Registry<T> : IDictionary<QualifiedIdentifier, T>, IReadOnlyDictionary<QualifiedIdentifier, T>
    where T : notnull
{
    /// <summary>The identifier of this registry itself, e.g. <c>core:blocks</c>.</summary>
    public QualifiedIdentifier Id { get; }

    protected Registry(QualifiedIdentifier id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    /// <summary>Key to value storage.</summary>
    protected abstract IDictionary<QualifiedIdentifier, T> Entries { get; }

    /// <summary>Value to key storage, kept in sync with <see cref="Entries"/>.</summary>
    protected abstract IReadOnlyDictionary<T, QualifiedIdentifier> ReverseEntries { get; }

    public abstract bool IsReadOnly { get; }

    public int Count => Entries.Count;

    public ICollection<QualifiedIdentifier> Keys => Entries.Keys;

    public ICollection<T> Values => Entries.Values;

    IEnumerable<QualifiedIdentifier> IReadOnlyDictionary<QualifiedIdentifier, T>.Keys => Keys;

    IEnumerable<T> IReadOnlyDictionary<QualifiedIdentifier, T>.Values => Values;

    /// <summary>Gets the value registered under <paramref name="key"/>, or adds/replaces it.</summary>
    /// <exception cref="KeyNotFoundException">Getting a key that is not registered.</exception>
    public T this[QualifiedIdentifier key]
    {
        get => TryGetValue(key, out T? value)
            ? value
            : throw new KeyNotFoundException($"'{key}' is not registered in '{Id}'.");
        set => Set(key, value);
    }

    public bool ContainsKey(QualifiedIdentifier key) => Entries.ContainsKey(key);

    public bool TryGetValue(QualifiedIdentifier key, [MaybeNullWhen(false)] out T value) =>
        Entries.TryGetValue(key, out value);

    public bool ContainsValue(T value) => ReverseEntries.ContainsKey(value);

    /// <summary>Tries to find the key that <paramref name="value"/> is registered under.</summary>
    public bool TryGetKey(T value, [NotNullWhen(true)] out QualifiedIdentifier? key) =>
        ReverseEntries.TryGetValue(value, out key);

    /// <summary>Gets the key that <paramref name="value"/> is registered under.</summary>
    /// <exception cref="KeyNotFoundException"><paramref name="value"/> is not registered.</exception>
    public QualifiedIdentifier GetKey(T value) =>
        TryGetKey(value, out QualifiedIdentifier? key)
            ? key
            : throw new KeyNotFoundException($"Value '{value}' is not registered in '{Id}'.");

    /// <summary>Registers <paramref name="value"/> under <paramref name="key"/>.</summary>
    /// <exception cref="ArgumentException">The key or the value is already registered.</exception>
    public abstract void Add(QualifiedIdentifier key, T value);

    /// <summary>Registers <paramref name="value"/> unless <paramref name="key"/> is already registered.</summary>
    /// <exception cref="ArgumentException">The value is already registered under another key.</exception>
    public abstract bool TryAdd(QualifiedIdentifier key, T value);

    public abstract bool Remove(QualifiedIdentifier key);

    public abstract void Clear();

    /// <summary>Adds or replaces the value under <paramref name="key"/>; backs the indexer setter.</summary>
    protected abstract void Set(QualifiedIdentifier key, T value);

    /// <summary>Returns a read-only snapshot of this registry. Later changes to this registry do not affect it.</summary>
    public virtual FrozenRegistry<T> Freeze() => new(Id, Entries);

    public IEnumerator<KeyValuePair<QualifiedIdentifier, T>> GetEnumerator() => Entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<KeyValuePair<QualifiedIdentifier, T>>.Add(KeyValuePair<QualifiedIdentifier, T> item) =>
        Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<QualifiedIdentifier, T>>.Contains(KeyValuePair<QualifiedIdentifier, T> item) =>
        TryGetValue(item.Key, out T? value) && EqualityComparer<T>.Default.Equals(value, item.Value);

    bool ICollection<KeyValuePair<QualifiedIdentifier, T>>.Remove(KeyValuePair<QualifiedIdentifier, T> item)
    {
        if (IsReadOnly)
        {
            throw ReadOnlyError();
        }

        return ((ICollection<KeyValuePair<QualifiedIdentifier, T>>)this).Contains(item) && Remove(item.Key);
    }

    void ICollection<KeyValuePair<QualifiedIdentifier, T>>.CopyTo(KeyValuePair<QualifiedIdentifier, T>[] array, int arrayIndex) =>
        Entries.CopyTo(array, arrayIndex);

    /// <summary>The exception thrown when modifying a read-only registry.</summary>
    protected NotSupportedException ReadOnlyError() => new($"Registry '{Id}' is frozen.");
}
