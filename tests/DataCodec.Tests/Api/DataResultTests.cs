using DataCodec.Api;

namespace DataCodec.Tests.Api;

public class DataResultTests
{
    [Fact]
    public void Success_HoldsValue()
    {
        var result = DataResult<int>.Success(5);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value);
        Assert.Null(result.Error);
        Assert.Equal(5, result.GetOrThrow());
        Assert.Equal("Success(5)", result.ToString());
    }

    [Fact]
    public void Success_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DataResult<string>.Success(null!));
    }

    [Fact]
    public void Fail_HoldsError()
    {
        var error = new InvalidCastException("not an int");
        var result = DataResult<int>.Fail(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError);
        Assert.Same(error, result.Error);
        Assert.Equal("Fail(InvalidCastException: not an int)", result.ToString());
    }

    [Fact]
    public void Fail_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DataResult<int>.Fail(null!));
    }

    [Fact]
    public void GetOrThrow_Failure_ThrowsStoredError()
    {
        var error = new KeyNotFoundException("missing");
        var result = DataResult<int>.Fail(error);

        var thrown = Assert.Throws<KeyNotFoundException>(() => result.GetOrThrow());
        Assert.Same(error, thrown);
    }

    [Fact]
    public void GetOrDefault_ReturnsValueOrFallback()
    {
        Assert.Equal(5, DataResult<int>.Success(5).GetOrDefault(1));
        Assert.Equal(1, DataResult<int>.Fail(new Exception()).GetOrDefault(1));
    }

    [Fact]
    public void TryGet_ReportsValueOrError()
    {
        var success = DataResult<string>.Success("a");
        var failure = DataResult<string>.Fail(new Exception("x"));

        Assert.True(success.TryGetValue(out var value));
        Assert.Equal("a", value);
        Assert.False(success.TryGetError(out _));

        Assert.False(failure.TryGetValue(out _));
        Assert.True(failure.TryGetError(out var error));
        Assert.Equal("x", error.Message);
    }

    [Fact]
    public void Map_TransformsSuccess()
    {
        var result = DataResult<int>.Success(5).Map(v => v.ToString());

        Assert.Equal("5", result.GetOrThrow());
    }

    [Fact]
    public void Map_PassesFailureThrough()
    {
        var error = new Exception("x");
        bool called = false;

        var result = DataResult<int>.Fail(error).Map(v => { called = true; return v.ToString(); });

        Assert.False(called);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void FlatMap_ChainsSuccessAndFailure()
    {
        var error = new Exception("x");

        Assert.Equal(10, DataResult<int>.Success(5).FlatMap(v => DataResult<int>.Success(v * 2)).GetOrThrow());
        Assert.Same(error, DataResult<int>.Success(5).FlatMap(_ => DataResult<int>.Fail(error)).Error);
    }

    [Fact]
    public void FlatMap_PassesFailureThrough()
    {
        var error = new Exception("x");
        bool called = false;

        var result = DataResult<int>.Fail(error).FlatMap(v => { called = true; return DataResult<int>.Success(v); });

        Assert.False(called);
        Assert.Same(error, result.Error);
    }
}
