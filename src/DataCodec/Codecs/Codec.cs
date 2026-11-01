using DataCodec.Codecs.Internal;

namespace DataCodec.Codecs;

/// <summary>The built-in codecs. Combine them with the methods in <see cref="CodecExtensions"/>.</summary>
public static partial class Codec
{
    public static ICodec<int> Int { get; } = new PrimitiveCodec<int>(d => d.GetInt(), (w, v) => w.WriteInt(v));

    public static ICodec<long> Long { get; } = new PrimitiveCodec<long>(d => d.GetLong(), (w, v) => w.WriteLong(v));

    public static ICodec<float> Float { get; } = new PrimitiveCodec<float>(d => d.GetFloat(), (w, v) => w.WriteFloat(v));

    public static ICodec<double> Double { get; } = new PrimitiveCodec<double>(d => d.GetDouble(), (w, v) => w.WriteDouble(v));

    public static ICodec<decimal> Decimal { get; } = new PrimitiveCodec<decimal>(d => d.GetDecimal(), (w, v) => w.WriteDecimal(v));

    public static ICodec<string> String { get; } = new PrimitiveCodec<string>(d => d.GetString(), (w, v) => w.WriteString(v));

    public static ICodec<bool> Bool { get; } = new PrimitiveCodec<bool>(d => d.GetBool(), (w, v) => w.WriteBool(v));

    /// <summary>Reads and writes <typeparamref name="TEnum"/> values by their exact, case-sensitive names.</summary>
    public static ICodec<TEnum> Enum<TEnum>() where TEnum : struct, System.Enum => new EnumCodec<TEnum>();
}
