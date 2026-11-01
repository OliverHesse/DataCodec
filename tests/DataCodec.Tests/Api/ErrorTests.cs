using DataCodec.Api.Deserialization;
using DataCodec.Api.Serialization;

namespace DataCodec.Tests.Api;

public class ErrorTests
{
    [Fact]
    public void DataDecodeException_FormatsMessage()
    {
        var inner = new FormatException("bad format");

        var error = new DataDecodeException("$.lives", "too low", inner);

        Assert.Equal("$.lives: too low", error.Message);
        Assert.Equal("$.lives", error.Path);
        Assert.Equal("too low", error.Reason);
        Assert.Same(inner, error.InnerException);
    }

    [Fact]
    public void DataEncodeException_ExposesReason()
    {
        var error = new DataEncodeException("$.x", "bad");

        Assert.Equal("bad", error.Reason);
        Assert.Equal("$.x: bad", error.Message);
    }
}
