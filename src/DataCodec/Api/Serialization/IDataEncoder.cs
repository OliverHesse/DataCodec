namespace DataCodec.Api.Serialization;

/// <summary>Turns a serialization tree into one file format.</summary>
public interface IDataEncoder
{
    /// <summary>The format's name, e.g. <c>json</c>.</summary>
    string Name { get; }

    /// <summary>Encodes <paramref name="root"/> and writes it to <paramref name="stream"/>.</summary>
    /// <exception cref="DataEncodeException">A value cannot be represented in this format.</exception>
    /// <exception cref="IOException">Writing to <paramref name="stream"/> failed.</exception>
    void Encode(SerializationElement root, Stream stream);

    /// <summary>Encodes <paramref name="root"/> as text. Encoding failures are returned, not thrown.</summary>
    DataResult<string> EncodeToString(SerializationElement root);

    /// <summary>
    /// Encodes <paramref name="root"/> and writes it to <paramref name="path"/>. The file is only written once
    /// encoding has succeeded, so a failure never leaves a half-written file behind.
    /// </summary>
    /// <exception cref="DataEncodeException">A value cannot be represented in this format.</exception>
    /// <exception cref="IOException">Writing the file failed.</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be written to.</exception>
    void EncodeToFile(SerializationElement root, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var buffer = new MemoryStream();
        Encode(root, buffer);
        File.WriteAllBytes(path, buffer.ToArray());
    }
}
