namespace DataCodec.Api.Serialization;

/// <summary>A value could not be written, e.g. <c>NaN</c> in JSON.</summary>
public sealed class DataEncodeException : Exception
{
    public DataEncodeException(string path, string message, Exception? innerException = null)
        : base($"{path}: {message}", innerException)
    {
        Path = path;
        Reason = message;
    }

    /// <summary>Where the failing value sits in the output, e.g. <c>$.stats.speed</c>.</summary>
    public string Path { get; }

    /// <summary>The message without the path.</summary>
    public string Reason { get; }

    /// <summary>Returns this error moved under <paramref name="parentPath"/>, e.g. <c>$.speed</c> under <c>$.stats</c>.</summary>
    public DataEncodeException Under(string parentPath) =>
        new(DataPath.Rebase(Path, parentPath), Reason, InnerException);
}
