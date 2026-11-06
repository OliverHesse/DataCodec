using DataCodec.Codecs.Internal;

namespace DataCodec.Codecs;

/// <summary>The built-in codecs. Combine them with the methods in <see cref="CodecExtensions"/>.</summary>
public static partial class Codec
{
    public static ICodec<int> Int { get; } = new PrimitiveCodec<int>(d => d.GetInt(), (w, v) => w.WriteInt(v));

    public static ICodec<long> Long { get; } = new PrimitiveCodec<long>(d => d.GetLong(), (w, v) => w.WriteLong(v));

    public static ICodec<float> Float { get; } = new PrimitiveCodec<float>(d => d.GetFloat(), (w, v) => w.WriteFloat(v));

    public static ICodec<double> Double { get; } = new PrimitiveCodec<double>(d => d.GetDouble(), (w, v) => w.WriteDouble(v));

    public static ICodec<decimal> Decimal { get; } = new PrimitiveCodec<decimal>(d => d.GetDecimal(), (w, v) => w.WriteDecimal(v));

    public static ICodec<string> String { get; } = new PrimitiveCodec<string>(d => d.GetString(), (w, v) => w.WriteString(v));

    public static ICodec<bool> Bool { get; } = new PrimitiveCodec<bool>(d => d.GetBool(), (w, v) => w.WriteBool(v));

    /// <summary>
    /// Starts a codec for a subtype of <typeparamref name="TBase"/> that is used as a codec for <typeparamref name="TBase"/>,
    /// e.g. <c>Codec.For&lt;IShape&gt;().Object(Codec.Double.Field("r", (Circle c) =&gt; c.R), r =&gt; new Circle(r))</c>.
    /// </summary>
    public static ObjectBuilder<TBase> For<TBase>() where TBase : notnull => default;

    /// <summary>
    /// A codec for a type with many implementations, stored as one flat object whose <paramref name="typeKey"/> names
    /// the implementation: <c>{"type": "circle", "r": 2}</c>. The rest of the object is read and written by the
    /// implementation's own map codec.
    /// </summary>
    /// <param name="keyCodec">Reads and writes the value under <paramref name="typeKey"/>, e.g. <see cref="String"/>.</param>
    /// <param name="keyOf">Gets the type key of a value, e.g. from a property or with a <c>switch</c> on its type.</param>
    /// <param name="lookup">Finds the codec for a type key, or returns null for an unknown key.</param>
    /// <param name="typeKey">The key that holds the type; <c>type</c> by default.</param>
    /// <remarks>
    /// <para>
    /// <paramref name="lookup"/> runs on every decode and encode, so types can be added after this codec is created,
    /// e.g. to a dictionary or a registry. Build each type's codec with <see cref="For{TBase}"/>.
    /// </para>
    /// <para>
    /// Decoding fails for data that is not an object, a missing or unknown type, or a type whose codec has its own
    /// <paramref name="typeKey"/> field. Encoding throws <see cref="Api.Serialization.DataEncodeException"/> in the
    /// same cases.
    /// </para>
    /// </remarks>
    public static ICodec<TBase> Dispatch<TBase, TKey>(
        ICodec<TKey> keyCodec,
        Func<TBase, TKey> keyOf,
        Func<TKey, IMapCodec<TBase>?> lookup,
        string typeKey = "type")
        where TBase : notnull where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keyCodec);
        ArgumentNullException.ThrowIfNull(keyOf);
        ArgumentNullException.ThrowIfNull(lookup);
        ArgumentException.ThrowIfNullOrEmpty(typeKey);
        return new DispatchCodec<TBase, TKey>(keyCodec, keyOf, lookup, typeKey);
    }

    /// <summary>
    /// A codec that contains itself, such as a tree node with child nodes. <paramref name="build"/> receives the codec
    /// being built and returns its definition:
    /// <code>
    /// Codec.Recursive&lt;Node&gt;(self =&gt; Codec.Object(
    ///     Codec.String.Field("name", (Node n) =&gt; n.Name),
    ///     self.ArrayOf().Optional("children", () =&gt; [], (Node n) =&gt; n.Children),
    ///     (name, children) =&gt; new Node(name, children)));
    /// </code>
    /// </summary>
    /// <remarks>
    /// <paramref name="build"/> may store the self reference in fields and collections but must not decode or encode
    /// with it; that throws <see cref="InvalidOperationException"/>, because the codec does not exist yet.
    /// </remarks>
    /// <exception cref="InvalidOperationException"><paramref name="build"/> used the codec, or returned null.</exception>
    public static ICodec<T> Recursive<T>(Func<ICodec<T>, ICodec<T>> build) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(build);
        return new RecursiveCodec<T>(build);
    }

    /// <summary>Reads and writes <typeparamref name="TEnum"/> values by their exact, case-sensitive names.</summary>
    public static ICodec<TEnum> Enum<TEnum>() where TEnum : struct, System.Enum => new EnumCodec<TEnum>();
}
