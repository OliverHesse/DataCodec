namespace DataCodec.Api.Serialization;

/// <summary>The exact type of value a <see cref="SerializationPrimitive"/> holds.</summary>
public enum PrimitiveKind
{
    Int,
    Long,
    Float,
    Double,
    Decimal,
    String,
    Bool,
    Null,
}

/// <summary>A single value in a serialization tree. Created through the <c>Write…</c> and <c>Append…</c> methods.</summary>
public sealed class SerializationPrimitive : SerializationElement
{
    internal SerializationPrimitive(PrimitiveKind kind, object? value)
    {
        Kind = kind;
        Value = value;
    }

    public PrimitiveKind Kind { get; }

    /// <summary>The boxed value; null only when <see cref="Kind"/> is <see cref="PrimitiveKind.Null"/>.</summary>
    public object? Value { get; }

    public override string ToString() => Kind == PrimitiveKind.Null ? "null" : $"{Value}";

    internal override void Encode(IDataWriter writer, string path)
    {
        try
        {
            switch (Kind)
            {
                case PrimitiveKind.Int: writer.WriteInt((int)Value!); break;
                case PrimitiveKind.Long: writer.WriteLong((long)Value!); break;
                case PrimitiveKind.Float: writer.WriteFloat((float)Value!); break;
                case PrimitiveKind.Double: writer.WriteDouble((double)Value!); break;
                case PrimitiveKind.Decimal: writer.WriteDecimal((decimal)Value!); break;
                case PrimitiveKind.String: writer.WriteString((string)Value!); break;
                case PrimitiveKind.Bool: writer.WriteBool((bool)Value!); break;
                case PrimitiveKind.Null: writer.WriteNull(); break;
            }
        }
        catch (Exception error) when (error is not DataEncodeException)
        {
            // Writers do not track paths, so attach this element's location here.
            throw new DataEncodeException(path, error.Message, error);
        }
    }
}
