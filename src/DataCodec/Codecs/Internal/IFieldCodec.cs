using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>A field with its value type erased, so an object codec can hold fields of different types.</summary>
internal interface IFieldCodec<TOwner> where TOwner : notnull
{
    string Key { get; }

    /// <summary>Reads this field's value from <paramref name="parent"/>, which is known to be an object.</summary>
    DataResult<object> Decode(IDataObject parent);

    /// <summary>Writes this field's key and value.</summary>
    void Encode(TOwner owner, IDataWriter writer);
}
