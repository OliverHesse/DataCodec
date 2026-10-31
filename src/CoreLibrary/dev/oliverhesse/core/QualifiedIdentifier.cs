using System.Diagnostics.CodeAnalysis;

namespace dev.oliverhesse.core;

/// <summary>
/// An immutable identifier made of a namespace and an identifier, written as <c>namespace:identifier</c>.
/// </summary>
public sealed class QualifiedIdentifier : IEquatable<QualifiedIdentifier>
{
    /// <summary>The namespace used by <see cref="Parse"/> when the input has no separator.</summary>
    public const string DefaultNamespace = "core";

    /// <summary>The character separating the namespace from the identifier.</summary>
    public const char Separator = ':';

    public string Namespace { get; }

    public string Identifier { get; }

    private QualifiedIdentifier(string ns, string identifier)
    {
        Namespace = ns;
        Identifier = identifier;
    }

    /// <summary>Creates a qualified identifier from a namespace and an identifier.</summary>
    /// <exception cref="ArgumentNullException">Either part is null.</exception>
    /// <exception cref="ArgumentException">Either part is empty or contains <see cref="Separator"/>.</exception>
    public static QualifiedIdentifier From(string ns, string identifier)
    {
        ArgumentNullException.ThrowIfNull(ns);
        ArgumentNullException.ThrowIfNull(identifier);

        string? error = Validate(ns, nameof(ns)) ?? Validate(identifier, nameof(identifier));
        if (error is not null)
        {
            throw new ArgumentException(error);
        }

        return new QualifiedIdentifier(ns, identifier);
    }

    /// <summary>Tries to create a qualified identifier from a namespace and an identifier.</summary>
    public static bool TryFrom(string? ns, string? identifier, [NotNullWhen(true)] out QualifiedIdentifier? result)
    {
        if (ns is null || identifier is null
            || Validate(ns, nameof(ns)) is not null
            || Validate(identifier, nameof(identifier)) is not null)
        {
            result = null;
            return false;
        }

        result = new QualifiedIdentifier(ns, identifier);
        return true;
    }

    /// <summary>
    /// Parses <c>namespace:identifier</c>. Input without a separator uses <see cref="DefaultNamespace"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="raw"/> is null.</exception>
    /// <exception cref="FormatException"><paramref name="raw"/> is not a valid qualified identifier.</exception>
    public static QualifiedIdentifier Parse(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        string? error = Split(raw, out string ns, out string identifier);
        if (error is not null)
        {
            throw new FormatException($"'{raw}' is not a valid qualified identifier: {error}");
        }

        return new QualifiedIdentifier(ns, identifier);
    }

    /// <summary>
    /// Tries to parse <c>namespace:identifier</c>. Input without a separator uses <see cref="DefaultNamespace"/>.
    /// </summary>
    public static bool TryParse(string? raw, [NotNullWhen(true)] out QualifiedIdentifier? result)
    {
        if (raw is null || Split(raw, out string ns, out string identifier) is not null)
        {
            result = null;
            return false;
        }

        result = new QualifiedIdentifier(ns, identifier);
        return true;
    }

    public bool NamespaceEquals(string? ns) => string.Equals(Namespace, ns, StringComparison.Ordinal);

    public bool NamespaceEquals(QualifiedIdentifier? other) => other is not null && NamespaceEquals(other.Namespace);

    public bool IdentifierEquals(string? identifier) => string.Equals(Identifier, identifier, StringComparison.Ordinal);

    public bool IdentifierEquals(QualifiedIdentifier? other) => other is not null && IdentifierEquals(other.Identifier);

    public bool Equals(QualifiedIdentifier? other) =>
        other is not null && NamespaceEquals(other) && IdentifierEquals(other);

    public override bool Equals(object? obj) => obj is QualifiedIdentifier other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Namespace, Identifier);

    public static bool operator ==(QualifiedIdentifier? left, QualifiedIdentifier? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(QualifiedIdentifier? left, QualifiedIdentifier? right) => !(left == right);

    public override string ToString() => $"{Namespace}{Separator}{Identifier}";

    /// <summary>Splits raw input into its parts, returning an error message or null on success.</summary>
    private static string? Split(string raw, out string ns, out string identifier)
    {
        int index = raw.IndexOf(Separator);
        if (index < 0)
        {
            ns = DefaultNamespace;
            identifier = raw;
        }
        else
        {
            ns = raw[..index];
            identifier = raw[(index + 1)..];
        }

        return Validate(ns, "namespace") ?? Validate(identifier, "identifier");
    }

    /// <summary>Checks a single part, returning an error message or null if it is valid.</summary>
    private static string? Validate(string part, string name)
    {
        if (part.Length == 0)
        {
            return $"{name} must not be empty.";
        }

        if (part.Contains(Separator))
        {
            return $"{name} must not contain '{Separator}'.";
        }

        return null;
    }
}
