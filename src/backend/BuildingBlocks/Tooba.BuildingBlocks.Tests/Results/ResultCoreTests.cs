using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Xunit;

namespace Tooba.BuildingBlocks.Tests.Results;

public sealed class ResultCoreTests
{
    [Fact]
    public void Success_has_no_errors_and_exposes_value()
    {
        var result = Result.Success(42);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
        Assert.Equal(42, result.Value);
        Assert.Throws<InvalidOperationException>(() => _ = result.FirstError);
    }

    [Fact]
    public void Failure_requires_errors_and_blocks_value()
    {
        var error = new SemanticError("demo.fail");
        var result = Result.Failure<int>(error);
        Assert.True(result.IsFailure);
        Assert.Equal("demo.fail", result.FirstError.Code);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Multi_error_failure_is_deterministic_primary_first()
    {
        var result = Result.Failure(
            new SemanticError("first"),
            new SemanticError("second"));
        Assert.Equal("first", result.FirstError.Code);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal("first", result.Match(() => "ok", errors => errors[0].Code));
        Assert.Equal("ok", Result.Success().Match(() => "ok", _ => "f"));
    }

    [Fact]
    public void Void_success_and_failure_match()
    {
        Assert.Equal("s", Result.Success().Match(() => "s", _ => "f"));
        Assert.Equal("f", Result.Failure(new SemanticError("x")).Match(() => "s", _ => "f"));
    }
}
