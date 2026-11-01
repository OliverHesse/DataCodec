namespace DataCodec.Api.Deserialization;

/// <summary>
/// Data could not be decoded for a reason a codec found, e.g. a value out of range or an unknown enum name.
/// Errors found by the reader itself (wrong type, missing key) keep their own exception types.
/// </summary>
public sealed class DataDecodeException : Exception
{
    public DataDecodeException(string path, string reason, Exception? innerException = null)
        : base($"{path}: {reason}", innerException)
    {
        Path = path;
        Reason = reason;
    }

    /// <summary>Where the failing data sits in the document, e.g. <c>$.players[2].lives</c>.</summary>
    public string Path { get; }

    /// <summary>The message without the path.</summary>
    public string Reason { get; }
}
