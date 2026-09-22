using Tooba.BuildingBlocks.Grid;
using Tooba.Settlement.Application.Errors;
using Tooba.Settlement.Application.Queries.QueryAdminPayoutGrid;
using Xunit;

namespace Tooba.Settlement.Tests.Behavior;

public sealed class SettlementErrorAndGridTests
{
    [Fact]
    public void Exception_mapper_maps_exact_stable_codes_only()
    {
        Assert.Equal(
            SettlementErrorCodes.AccountMissing,
            SettlementExceptionMapper.ToSemanticError(new InvalidOperationException(SettlementErrorCodes.AccountMissing)).Code);
        Assert.Equal(
            SettlementErrorCodes.PayoutInvalidAmount,
            SettlementExceptionMapper.ToSemanticError(new InvalidOperationException(SettlementErrorCodes.AmountInvalid)).Code);
        Assert.Equal(
            SettlementErrorCodes.PayoutMissing,
            SettlementExceptionMapper.ToSemanticError(new InvalidOperationException(SettlementErrorCodes.PayoutMissing)).Code);
        Assert.Equal(
            SettlementErrorCodes.GatewayUnconfigured,
            SettlementExceptionMapper.ToSemanticError(new InvalidOperationException(SettlementErrorCodes.GatewayUnconfigured)).Code);
        Assert.Equal(
            "settlement.restore.payout_completed",
            SettlementExceptionMapper.ToSemanticError(new InvalidOperationException("settlement.restore.payout_completed")).Code);
    }

    [Fact]
    public void Exception_mapper_does_not_parse_localized_or_prose_messages()
    {
        Assert.False(SettlementExceptionMapper.TryMapExact("پیدا نشد", out _));
        Assert.False(SettlementExceptionMapper.TryMapExact("Payout failed somehow", out _));
        Assert.False(SettlementExceptionMapper.TryMapExact("settlement.unknown.future_code", out _));
        Assert.Throws<InvalidOperationException>(() =>
            SettlementExceptionMapper.ToSemanticError(new InvalidOperationException("پیدا نشد")));
    }

    [Fact]
    public async Task Exception_mapper_propagates_unknown_InvalidOperationException()
    {
        var unknown = new InvalidOperationException("settlement.pricing.unexpected");
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SettlementExceptionMapper.TryAsync<int>(() => throw unknown));
        Assert.Same(unknown, thrown);
        Assert.Equal("settlement.pricing.unexpected", thrown.Message);
    }

    [Fact]
    public void Payout_grid_policy_preserves_default_sort_and_field_whitelist()
    {
        var normalized = AdminPayoutGridQueryPolicy.Instance.Normalize(
            new GridQueryRequest(1, 20, null, [], [], null));
        Assert.Equal("created", Assert.Single(normalized.Sort).Field);
        Assert.Equal("desc", normalized.Sort[0].Direction);

        Assert.Throws<GridQueryValidationException>(() =>
            AdminPayoutGridQueryPolicy.Instance.Normalize(
                new GridQueryRequest(
                    1,
                    20,
                    null,
                    [],
                    [new GridFilterRequest("not-a-field", "equals", "x", null, null)],
                    null)));
    }
}
