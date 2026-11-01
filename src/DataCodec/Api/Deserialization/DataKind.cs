namespace DataCodec.Api.Deserialization;

/// <summary>What an <see cref="IDataObject"/> holds.</summary>
public enum DataKind
{
    /// <summary>A collection of key-value pairs.</summary>
    Object,

    /// <summary>A list of data objects.</summary>
    Array,

    /// <summary>A single value such as a number, string or bool.</summary>
    Value,

    /// <summary>A null value.</summary>
    Null,
}
