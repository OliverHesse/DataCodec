namespace DataCodec.Api.Deserialization;

/// <summary>
/// Reads one file format into an <see cref="IDataObject"/>. Invalid content is returned as a failed result;
/// I/O problems are thrown.
/// </summary>
public interface IDataDecoder
{
    /// <summary>The format's name, e.g. <c>json</c>.</summary>
    string Name { get; }

    DataResult<IDataObject> Decode(string text);

    /// <exception cref="IOException">Reading from <paramref name="stream"/> failed.</exception>
    DataResult<IDataObject> Decode(Stream stream);

    /// <summary>Reads and decodes the file at <paramref name="path"/>.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be read.</exception>
    /// <exception cref="IOException">Reading the file failed.</exception>
    DataResult<IDataObject> DecodeFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using FileStream stream = File.OpenRead(path);
        return Decode(stream);
    }
}
