using System.Collections.Frozen;

namespace CoreLibrary;

/// <summary>
/// A read-only snapshot of a registry, created by <see cref="Registry{T}.Freeze"/>.
/// Every modify method throws <see cref="NotSupportedException"/>.
/// </summary>
public sealed class FrozenRegistry<T> : Registry<T> where T : notnull
{
    private readonly FrozenDictionary<QualifiedIdentifier, T> _entries;
    private readonly FrozenDictionary<T, QualifiedIdentifier> _reverseEntries;

    internal FrozenRegistry(QualifiedIdentifier id, IEnumerable<KeyValuePair<QualifiedIdentifier, T>> entries) : base(id)
    {
        _entries = entries.ToFrozenDictionary();
        _reverseEntries = _entries.ToFrozenDictionary(entry => entry.Value, entry => entry.Key);
    }

    protected override IDictionary<QualifiedIdentifier, T> Entries => _entries;

    protected override IReadOnlyDictionary<T, QualifiedIdentifier> ReverseEntries => _reverseEntries;

    public override bool IsReadOnly => true;

    public override void Add(QualifiedIdentifier key, T value) => throw ReadOnlyError();

    public override bool TryAdd(QualifiedIdentifier key, T value) => throw ReadOnlyError();

    public override bool Remove(QualifiedIdentifier key) => throw ReadOnlyError();

    public override void Clear() => throw ReadOnlyError();

    protected override void Set(QualifiedIdentifier key, T value) => throw ReadOnlyError();

    /// <summary>A frozen registry never changes, so it is its own snapshot.</summary>
    public override FrozenRegistry<T> Freeze() => this;
}
