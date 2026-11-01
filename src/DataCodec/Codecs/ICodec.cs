using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs;

/// <summary>
/// One definition of how <typeparamref name="T"/> maps to data, in both directions. Codecs are composed from the
/// built-ins on <see cref="Codec"/> and <see cref="CodecExtensions"/>.
/// </summary>
public interface ICodec<T> where T : notnull
{
    /// <summary>Reads a <typeparamref name="T"/>. Bad data gives a failed result with its path; it never throws.</summary>
    DataResult<T> Decode(IDataObject data);

    /// <summary>Writes only this value to <paramref name="writer"/>; composite codecs write their structure around it.</summary>
    /// <exception cref="DataEncodeException">The value cannot be written.</exception>
    void Encode(T value, IDataWriter writer);
}
