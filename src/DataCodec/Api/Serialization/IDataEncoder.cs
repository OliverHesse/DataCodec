using System.Text;

namespace DataCodec.Api.Serialization;

/// <summary>
/// Writes one file format. A format only implements <see cref="Encode(Stream, Action{IDataWriter})"/>, which hands
/// out an <see cref="IDataWriter"/>; every other method is built on it.
/// </summary>
public interface IDataEncoder
{
    /// <summary>The format's name, e.g. <c>json</c>.</summary>
    string Name { get; }

    /// <summary>Calls <paramref name="write"/> with a writer for this format that writes to <paramref name="stream"/>.</summary>
    /// <exception cref="DataEncodeException">A value cannot be represented in this format.</exception>
    /// <exception cref="IOException">Writing to <paramref name="stream"/> failed.</exception>
    void Encode(Stream stream, Action<IDataWriter> write);

    /// <summary>Encodes <paramref name="root"/> and writes it to <paramref name="stream"/>.</summary>
    /// <exception cref="DataEncodeException">A value cannot be represented in this format.</exception>
    /// <exception cref="IOException">Writing to <paramref name="stream"/> failed.</exception>
    void Encode(SerializationElement root, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(root);
        Encode(stream, root.Encode);
    }

    /// <summary>Encodes whatever <paramref name="write"/> writes as text. Encoding failures are returned, not thrown.</summary>
    DataResult<string> EncodeToString(Action<IDataWriter> write)
    {
        using var buffer = new MemoryStream();
        try
        {
            Encode(buffer, write);
        }
        catch (DataEncodeException error)
        {
            return DataResult<string>.Fail(error);
        }

        return DataResult<string>.Success(Encoding.UTF8.GetString(buffer.ToArray()));
    }

    /// <summary>Encodes <paramref name="root"/> as text. Encoding failures are returned, not thrown.</summary>
    DataResult<string> EncodeToString(SerializationElement root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return EncodeToString(root.Encode);
    }

    /// <summary>
    /// Encodes whatever <paramref name="write"/> writes and saves it to <paramref name="path"/>. The file is only
    /// written once encoding has succeeded, so a failure never leaves a half-written file behind.
    /// </summary>
    /// <exception cref="DataEncodeException">A value cannot be represented in this format.</exception>
    /// <exception cref="IOException">Writing the file failed.</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be written to.</exception>
    void EncodeToFile(string path, Action<IDataWriter> write)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var buffer = new MemoryStream();
        Encode(buffer, write);
        File.WriteAllBytes(path, buffer.ToArray());
    }

    /// <summary>Encodes <paramref name="root"/> and saves it to <paramref name="path"/>; nothing is written if encoding fails.</summary>
    /// <exception cref="DataEncodeException">A value cannot be represented in this format.</exception>
    /// <exception cref="IOException">Writing the file failed.</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be written to.</exception>
    void EncodeToFile(SerializationElement root, string path)
    {
        ArgumentNullException.ThrowIfNull(root);
        EncodeToFile(path, root.Encode);
    }
}
