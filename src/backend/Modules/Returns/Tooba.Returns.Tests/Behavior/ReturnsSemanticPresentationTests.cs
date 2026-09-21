using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Contracts;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

public sealed class ReturnsSemanticPresentationTests
{
    [Fact]
    public void Invalid_refund_destination_is_central_semantic_failure()
    {
        var result = ReturnSemanticMapper.ParseDestination("not-a-destination");
        Assert.True(result.IsFailure);
        Assert.Equal(ReturnsErrorCodes.RefundDestinationInvalid, result.Errors[0].Code);
    }

    [Fact]
    public void Valid_refund_destination_parses()
    {
        var result = ReturnSemanticMapper.ParseDestination(nameof(RefundDestination.Wallet));
        Assert.True(result.IsSuccess);
        Assert.Equal(RefundDestination.Wallet, result.Value);
    }

    [Fact]
    public void Return_missing_maps_to_central_code()
    {
        var error = ReturnSemanticMapper.MapException(new InvalidOperationException("درخواست مرجوعی پیدا نشد."));
        Assert.Equal(ReturnsErrorCodes.Missing, error.Code);
    }

    [Fact]
    public void Seller_approve_reject_expected_failures_map_without_raw_message()
    {
        var stale = ReturnSemanticMapper.MapException(new InvalidOperationException("انتقال وضعیت از این حالت مجاز نیست."));
        Assert.Equal(ReturnsErrorCodes.Stale, stale.Code);
        Assert.DoesNotContain("انتقال", stale.Code, StringComparison.Ordinal);
    }
}
