using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Errors;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

public sealed class ReturnsSemanticPresentationTests
{
    [Fact]
    public void Invalid_refund_destination_is_central_semantic_failure()
    {
        var result = ReturnsExceptionMapper.ParseDestination("not-a-destination");
        Assert.True(result.IsFailure);
        Assert.Equal(ReturnsErrorCodes.RefundDestinationInvalid, result.Errors[0].Code);
    }

    [Fact]
    public void Valid_refund_destination_parses()
    {
        var result = ReturnsExceptionMapper.ParseDestination(nameof(RefundDestination.Wallet));
        Assert.True(result.IsSuccess);
        Assert.Equal(RefundDestination.Wallet, result.Value);
    }

    [Fact]
    public void Stable_directory_codes_map_without_raw_message()
    {
        var missing = ReturnsExceptionMapper.ToSemanticError(
            new InvalidOperationException("returns.request.not_found"));
        Assert.Equal(ReturnsErrorCodes.Missing, missing.Code);

        var stale = ReturnsExceptionMapper.ToSemanticError(
            new InvalidOperationException("fulfillment.status.transition_invalid"));
        Assert.Equal(ReturnsErrorCodes.Stale, stale.Code);
        Assert.DoesNotContain("انتقال", stale.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void Prose_and_unknown_codes_are_not_swallowed()
    {
        Assert.False(ReturnsExceptionMapper.TryMapExact("درخواست مرجوعی پیدا نشد.", out _));
        Assert.False(ReturnsExceptionMapper.TryMapExact("انتقال وضعیت از این حالت مجاز نیست.", out _));
        Assert.False(ReturnsExceptionMapper.TryMapExact("returns.unknown.future_code", out _));
        Assert.Throws<InvalidOperationException>(() =>
            ReturnsExceptionMapper.ToSemanticError(new InvalidOperationException("پیدا نشد")));
    }

    [Fact]
    public async Task Unexpected_InvalidOperationException_propagates_from_TryAsync()
    {
        var unknown = new InvalidOperationException("returns.unknown.future_code");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ReturnsExceptionMapper.TryAsync<int>(() => throw unknown));
    }
}
