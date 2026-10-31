namespace DataCodec.Api.Serialization;

/// <summary>
/// A node in a format-independent tree of data to write: a <see cref="SerializationObject"/>,
/// <see cref="SerializationArray"/> or <see cref="SerializationPrimitive"/>.
/// </summary>
/// <remarks>
/// Elements are always created by their parent and never attached from elsewhere.
/// Call <see cref="EncodeToString"/> on the root to turn the whole tree into text for one format.
/// </remarks>
public abstract class SerializationElement
{
    private protected SerializationElement()
    {
    }

    /// <summary>Writes this element, then its children, to <paramref name="writer"/>.</summary>
    /// <exception cref="DataEncodeException">The writer cannot represent a value in the tree.</exception>
    public void Encode(IDataWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Encode(writer, DataPath.Root);
    }

    /// <summary>Encodes a tree as text. Encoding failures are returned, not thrown.</summary>
    public DataResult<string> EncodeToString(IDataEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        return encoder.EncodeToString(this);
    }

    /// <exception cref="DataEncodeException">A value cannot be represented in the encoder's format.</exception>
    /// <exception cref="IOException">Writing to <paramref name="stream"/> failed.</exception>
    public void EncodeTo(IDataEncoder encoder, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        encoder.Encode(this, stream);
    }

    /// <summary>Writes this tree to a file; nothing is written if encoding fails.</summary>
    /// <exception cref="DataEncodeException">A value cannot be represented in the encoder's format.</exception>
    /// <exception cref="IOException">Writing the file failed.</exception>
    public void EncodeToFile(IDataEncoder encoder, string path)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        encoder.EncodeToFile(this, path);
    }

    /// <summary>Writes this element; <paramref name="path"/> is its location, used only in error messages.</summary>
    internal abstract void Encode(IDataWriter writer, string path);
}
