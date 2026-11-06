namespace DataCodec.Codecs;

/// <summary>
/// Builds codecs for subtypes of <typeparamref name="TBase"/> that are used as codecs for <typeparamref name="TBase"/>
/// itself. Get one from <see cref="Codec.For{TBase}"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <c>Object</c> methods match <see cref="Codec.Object{TOwner, T1}"/>, except that the owner type is inferred from
/// the field getters, so the getters take the real subtype and need no casts:
/// <code>
/// IMapCodec&lt;IShape&gt; circle = Codec.For&lt;IShape&gt;().Object(
///     Codec.Double.Field("r", (Circle c) =&gt; c.R),
///     r =&gt; new Circle(r));
/// </code>
/// </para>
/// <para>
/// Decoding returns the subtype as a <typeparamref name="TBase"/>. Encoding a <typeparamref name="TBase"/> that is not
/// the codec's subtype throws <see cref="Api.Serialization.DataEncodeException"/>.
/// </para>
/// <para>
/// This is a separate step because C# cannot take <typeparamref name="TBase"/> explicitly and infer the other type
/// arguments in one call. The builder holds no state and allocates nothing.
/// </para>
/// </remarks>
public readonly partial struct ObjectBuilder<TBase> where TBase : notnull
{
    /// <summary>Uses an existing map codec for <typeparamref name="TSub"/> as a map codec for <typeparamref name="TBase"/>.</summary>
    public IMapCodec<TBase> From<TSub>(IMapCodec<TSub> codec) where TSub : notnull, TBase =>
        codec.Upcast<TSub, TBase>();

    /// <summary>Uses an existing codec for <typeparamref name="TSub"/> as a codec for <typeparamref name="TBase"/>.</summary>
    public ICodec<TBase> From<TSub>(ICodec<TSub> codec) where TSub : notnull, TBase =>
        codec.Upcast<TSub, TBase>();
}
