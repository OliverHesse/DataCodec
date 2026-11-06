using System.Diagnostics.CodeAnalysis;

namespace DataCodec;

/// <summary>
/// The outcome of reading data: either a non-null value or the <see cref="Exception"/> describing why it failed.
/// </summary>
/// <remarks>
/// Failures are returned rather than thrown. Call <see cref="GetOrThrow"/> to turn a failure into an exception.
/// </remarks>
public sealed class DataResult<T> where T : notnull
{
    /// <summary>The value on success, otherwise null.</summary>
    public T? Value { get; }

    /// <summary>The error on failure, otherwise null.</summary>
    public Exception? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsError => !IsSuccess;

    private DataResult(T? value, Exception? error, bool isSuccess)
    {
        Value = value;
        Error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>Creates a successful result.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static DataResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new DataResult<T>(value, null, true);
    }

    /// <summary>Creates a failed result.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is null.</exception>
    public static DataResult<T> Fail(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new DataResult<T>(default, error, false);
    }

    /// <summary>
    /// Creates a failed result whose error is a <see cref="Api.Deserialization.DataDecodeException"/> at
    /// <paramref name="path"/>, e.g. <c>Fail("$.lives", "must be positive")</c>. Build paths with <see cref="DataPath"/>.
    /// </summary>
    public static DataResult<T> Fail(string path, string reason) =>
        Fail(new Api.Deserialization.DataDecodeException(path, reason));

    /// <summary>Returns the value, or throws <see cref="Error"/> if this result failed.</summary>
    public T GetOrThrow() => IsSuccess ? Value : throw Error;

    public T GetOrDefault(T fallback) => IsSuccess ? Value : fallback;

    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = Value;
        return IsSuccess;
    }

    public bool TryGetError([NotNullWhen(true)] out Exception? error)
    {
        error = Error;
        return IsError;
    }

    /// <summary>Transforms the value on success; a failure is passed through unchanged.</summary>
    public DataResult<TOut> Map<TOut>(Func<T, TOut> mapper) where TOut : notnull =>
        IsSuccess ? DataResult<TOut>.Success(mapper(Value)) : DataResult<TOut>.Fail(Error);

    /// <summary>Chains another operation that can fail; a failure is passed through unchanged.</summary>
    public DataResult<TOut> FlatMap<TOut>(Func<T, DataResult<TOut>> mapper) where TOut : notnull =>
        IsSuccess ? mapper(Value) : DataResult<TOut>.Fail(Error);

    public override string ToString() =>
        IsSuccess ? $"Success({Value})" : $"Fail({Error.GetType().Name}: {Error.Message})";
}
