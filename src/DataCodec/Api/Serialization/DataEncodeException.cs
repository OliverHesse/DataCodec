namespace DataCodec.Api.Serialization;

/// <summary>A value in a serialization tree could not be written, e.g. <c>NaN</c> in JSON.</summary>
public sealed class DataEncodeException : Exception
{
    public DataEncodeException(string path, string message, Exception? innerException = null)
        : base($"{path}: {message}", innerException)
    {
        Path = path;
    }

    /// <summary>Where the failing element sits in the tree, e.g. <c>$.stats.speed</c>.</summary>
    public string Path { get; }
}
