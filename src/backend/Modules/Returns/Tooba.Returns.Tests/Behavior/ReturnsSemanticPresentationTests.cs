using Tooba.BuildingBlocks;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

public sealed class ReturnsSemanticPresentationTests
{
    [Fact]
    public void Invalid_refund_destination_is_central_semantic_failure()
    {
        var result = ReturnRefundDestinationParser.Parse("not-a-destination");
        Assert.True(result.IsFailure);
        Assert.Equal(ReturnsErrorCodes.RefundDestinationInvalid, result.Errors[0].Code);
    }

    [Fact]
    public void Valid_refund_destination_parses()
    {
        var result = ReturnRefundDestinationParser.Parse(nameof(RefundDestination.Wallet));
        Assert.True(result.IsSuccess);
        Assert.Equal(RefundDestination.Wallet, result.Value);
    }

    [Fact]
    public void Absent_refund_destination_keeps_shipped_default()
    {
        var result = ReturnRefundDestinationParser.Parse(null);
        Assert.True(result.IsSuccess);
        Assert.Equal(RefundDestination.OriginalPayment, result.Value);
    }

    [Fact]
    public void Stable_directory_codes_map_by_typed_code_not_message()
    {
        var missing = ReturnsOperation.ToSemanticError(new ContractOperationException(ReturnsErrorCodes.Missing));
        Assert.Equal(ReturnsErrorCodes.Missing, missing.Code);

        var stale = ReturnsOperation.ToSemanticError(new ContractOperationException(ReturnsErrorCodes.Stale));
        Assert.Equal(ReturnsErrorCodes.Stale, stale.Code);
        Assert.DoesNotContain("انتقال", stale.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_codes_are_not_swallowed()
    {
        Assert.False(ReturnsErrorCodes.IsKnown("returns.unknown.future_code"));
        Assert.False(ReturnsErrorCodes.IsKnown("درخواست مرجوعی پیدا نشد."));
        Assert.Throws<ContractOperationException>(() =>
            ReturnsOperation.ToSemanticError(new ContractOperationException("returns.unknown.future_code")));
    }

    [Fact]
    public async Task Unexpected_ContractOperationException_propagates_from_typed_seam()
    {
        await Assert.ThrowsAsync<ContractOperationException>(() =>
            ReturnsOperation.ExecuteAsync<int>(() =>
                throw new ContractOperationException("returns.unknown.future_code")));
    }

    [Fact]
    public async Task Known_ContractOperationException_maps_to_result_failure()
    {
        var result = await ReturnsOperation.ExecuteAsync<int>(() =>
            throw new ContractOperationException(ReturnsErrorCodes.QuantityExceeded));
        Assert.True(result.IsFailure);
        Assert.Equal(ReturnsErrorCodes.QuantityExceeded, result.Errors[0].Code);
    }
}
