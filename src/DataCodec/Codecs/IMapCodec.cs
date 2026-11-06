using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs;

/// <summary>
/// A codec for a value stored as the fields of an object. Besides reading and writing a whole object, it can read and
/// write just its fields, so another codec can put them in an object it opened itself, e.g. next to a <c>type</c> key.
/// </summary>
/// <remarks>Created by <see cref="Codec.Object{TOwner, T1}"/>; <c>Xmap</c> and <c>Upcast</c> keep a map codec a map codec.</remarks>
public interface IMapCodec<T> : ICodec<T> where T : notnull
{
    /// <summary>The keys this codec reads and writes, in write order.</summary>
    IReadOnlyList<string> Keys { get; }

    /// <summary>
    /// Reads a <typeparamref name="T"/> from the fields of <paramref name="data"/>, which is known to be an object.
    /// Keys this codec does not know are ignored. Bad data gives a failed result with its path; it never throws.
    /// </summary>
    DataResult<T> DecodeFields(IDataObject data);

    /// <summary>
    /// Writes the key and value of each field, without <see cref="IDataWriter.WriteStartObject"/> or
    /// <see cref="IDataWriter.WriteEndObject"/>; the caller has opened the object and closes it.
    /// </summary>
    /// <exception cref="DataEncodeException">A value cannot be written.</exception>
    void EncodeFields(T value, IDataWriter writer);
}
