using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;
using DataCodec.Codecs.Internal;

namespace DataCodec.Codecs;

/// <summary>Builds new codecs from existing ones, and reads or writes values with a codec.</summary>
public static partial class CodecExtensions
{
    /// <summary>
    /// Converts this codec's values: <paramref name="decode"/> runs after reading, <paramref name="encode"/> before
    /// writing. An exception from <paramref name="decode"/> becomes a failed result.
    /// </summary>
    public static ICodec<B> Xmap<A, B>(this ICodec<A> codec, Func<A, B> decode, Func<B, A> encode)
        where A : notnull where B : notnull
    {
        ArgumentNullException.ThrowIfNull(codec);
        ArgumentNullException.ThrowIfNull(decode);
        ArgumentNullException.ThrowIfNull(encode);
        return new XmapCodec<A, B>(codec, decode, encode);
    }

    /// <summary>
    /// A required field of an object codec. Decoding fails when <paramref name="key"/> is missing or its value is null.
    /// </summary>
    public static FieldCodec<TOwner, T> Field<TOwner, T>(this ICodec<T> codec, string key, Func<TOwner, T> getter)
        where TOwner : notnull where T : notnull
    {
        ArgumentNullException.ThrowIfNull(codec);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(getter);
        return new FieldCodec<TOwner, T>(key, codec, getter, defaultFactory: null);
    }

    /// <summary>
    /// An optional field of an object codec. When <paramref name="key"/> is missing or its value is null, decoding
    /// uses <paramref name="defaultValue"/>; a value of the wrong type still fails. The value is always written.
    /// </summary>
    /// <remarks>
    /// Every decode returns this same instance. For a mutable default such as a list, use the overload that takes a
    /// factory, so decoded objects do not share it.
    /// </remarks>
    public static FieldCodec<TOwner, T> Optional<TOwner, T>(
        this ICodec<T> codec, string key, T defaultValue, Func<TOwner, T> getter)
        where TOwner : notnull where T : notnull
    {
        ArgumentNullException.ThrowIfNull(defaultValue);
        return codec.Optional(key, () => defaultValue, getter);
    }

    /// <summary>
    /// An optional field of an object codec. When <paramref name="key"/> is missing or its value is null, decoding
    /// calls <paramref name="defaultFactory"/> for a fresh default; a value of the wrong type still fails.
    /// </summary>
    public static FieldCodec<TOwner, T> Optional<TOwner, T>(
        this ICodec<T> codec, string key, Func<T> defaultFactory, Func<TOwner, T> getter)
        where TOwner : notnull where T : notnull
    {
        ArgumentNullException.ThrowIfNull(codec);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(defaultFactory);
        ArgumentNullException.ThrowIfNull(getter);
        return new FieldCodec<TOwner, T>(key, codec, getter, defaultFactory);
    }

    /// <summary>A codec for a list of this codec's values, read from and written to an array.</summary>
    public static ICodec<List<T>> ListOf<T>(this ICodec<T> codec) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(codec);
        return new ListCodec<T>(codec);
    }

    /// <summary>A codec for a string-keyed dictionary of this codec's values, read from and written to an object.</summary>
    public static ICodec<Dictionary<string, T>> DictionaryOf<T>(this ICodec<T> codec) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(codec);
        return new DictionaryCodec<T>(codec);
    }

    /// <summary>Rejects values for which <paramref name="predicate"/> is false: a failed result when decoding, a throw when encoding.</summary>
    public static ICodec<T> Validate<T>(this ICodec<T> codec, Func<T, bool> predicate, string reason) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(reason);
        return codec.Validate(predicate, _ => reason);
    }

    public static ICodec<T> Min<T>(this ICodec<T> codec, T min) where T : notnull, IComparable<T> =>
        codec.Validate(v => v.CompareTo(min) >= 0, v => BelowMinimum(v, min));

    public static ICodec<T> Max<T>(this ICodec<T> codec, T max) where T : notnull, IComparable<T> =>
        codec.Validate(v => v.CompareTo(max) <= 0, v => AboveMaximum(v, max));

    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static ICodec<T> Range<T>(this ICodec<T> codec, T min, T max) where T : notnull, IComparable<T>
    {
        EnsureOrdered(min, max);
        return codec.Validate(
            v => v.CompareTo(min) >= 0 && v.CompareTo(max) <= 0,
            v => v.CompareTo(min) < 0 ? BelowMinimum(v, min) : AboveMaximum(v, max));
    }

    /// <summary>Moves values outside <paramref name="min"/>..<paramref name="max"/> to the nearest bound, in both directions.</summary>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static ICodec<T> Clamp<T>(this ICodec<T> codec, T min, T max) where T : notnull, IComparable<T>
    {
        EnsureOrdered(min, max);
        T clamp(T v) => v.CompareTo(min) < 0 ? min : v.CompareTo(max) > 0 ? max : v;
        return codec.Xmap(clamp, clamp);
    }
    /// <summary>Decodes <paramref name="text"/> with <paramref name="decoder"/>, then reads it with this codec.</summary>
    public static DataResult<T> Decode<T>(this ICodec<T> codec, string text, IDataDecoder decoder) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(decoder);
        return decoder.Decode(text).FlatMap(codec.Decode);
    }

    /// <summary>Decodes the file at <paramref name="path"/>, then reads it with this codec.</summary>
    /// <exception cref="IOException">Reading the file failed.</exception>
    public static DataResult<T> DecodeFile<T>(this ICodec<T> codec, IDataDecoder decoder, string path) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(decoder);
        return decoder.DecodeFile(path).FlatMap(codec.Decode);
    }

    /// <summary>Encodes <paramref name="value"/> as text. Encoding failures are returned, not thrown.</summary>
    public static DataResult<string> EncodeToString<T>(this ICodec<T> codec, T value, IDataEncoder encoder) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(encoder);
        return encoder.EncodeToString(writer => codec.Encode(value, writer));
    }

    /// <exception cref="DataEncodeException">The value cannot be written.</exception>
    /// <exception cref="IOException">Writing to <paramref name="stream"/> failed.</exception>
    public static void EncodeTo<T>(this ICodec<T> codec, T value, IDataEncoder encoder, Stream stream) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(encoder);
        encoder.Encode(stream, writer => codec.Encode(value, writer));
    }

    /// <summary>Encodes <paramref name="value"/> to a file; nothing is written if encoding fails.</summary>
    /// <exception cref="DataEncodeException">The value cannot be written.</exception>
    /// <exception cref="IOException">Writing the file failed.</exception>
    public static void EncodeToFile<T>(this ICodec<T> codec, T value, IDataEncoder encoder, string path) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(encoder);
        encoder.EncodeToFile(path, writer => codec.Encode(value, writer));
    }

    private static ICodec<T> Validate<T>(this ICodec<T> codec, Func<T, bool> predicate, Func<T, string> reason)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(codec);
        ArgumentNullException.ThrowIfNull(predicate);
        return new ValidatedCodec<T>(codec, predicate, reason);
    }

    private static string BelowMinimum<T>(T value, T min) => $"{value} is below the minimum {min}";

    private static string AboveMaximum<T>(T value, T max) => $"{value} is above the maximum {max}";

    private static void EnsureOrdered<T>(T min, T max) where T : IComparable<T>
    {
        if (min.CompareTo(max) > 0)
        {
            throw new ArgumentException($"The minimum {min} is greater than the maximum {max}.");
        }
    }
}
