namespace DataCodec.Api;

/// <summary>A format-independent list of data objects.</summary>
public interface IDataArray
{
    int Length { get; }

    IDataObject[] AsArray();
}
