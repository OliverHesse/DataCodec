using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Codecs.Internal;

/// <summary>Reads and writes an enum by its exact name. Numbers and combined flag values are rejected.</summary>
internal sealed class EnumCodec<TEnum> : ICodec<TEnum> where TEnum : struct, Enum
{
    private static readonly HashSet<string> Names = new(Enum.GetNames<TEnum>(), StringComparer.Ordinal);

    public DataResult<TEnum> Decode(IDataObject data) => data.GetString().FlatMap(name =>
        // Enum.TryParse alone would also accept "1", "red" and "Red, Green".
        Names.Contains(name)
            ? DataResult<TEnum>.Success(Enum.Parse<TEnum>(name))
            : DataResult<TEnum>.Fail(new DataDecodeException(data.Path, $"'{name}' is not a valid {typeof(TEnum).Name}")));

    public void Encode(TEnum value, IDataWriter writer)
    {
        string? name = Enum.GetName(value)
            ?? throw new DataEncodeException(DataPath.Root, $"{value} is not a named {typeof(TEnum).Name} value");

        writer.WriteString(name);
    }
}
