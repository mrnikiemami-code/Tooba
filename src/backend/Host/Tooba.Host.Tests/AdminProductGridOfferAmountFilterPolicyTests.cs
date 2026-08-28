using Tooba.BuildingBlocks;
using Tooba.Host.Grid;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class AdminProductGridOfferAmountFilterPolicyTests
{
    [Fact]
    public void Normalize_accepts_offerAmountRange_number_filter()
    {
        var normalized = AdminProductGridQueryPolicy.Normalize(new GridQueryRequest(
            1,
            20,
            null,
            [],
            [new GridFilterRequest("offerAmountRange", "greaterThanOrEqual", "1000", null, null)],
            null));

        Assert.Single(normalized.Filters);
        Assert.Equal("offerAmountRange", normalized.Filters[0].Field);
        Assert.Equal("greaterThanOrEqual", normalized.Filters[0].Operator);
    }

    [Fact]
    public void Normalize_rejects_invalid_offerAmountRange_operator()
    {
        var ex = Assert.Throws<PlatformHttpException>(() => AdminProductGridQueryPolicy.Normalize(new GridQueryRequest(
            1,
            20,
            null,
            [],
            [new GridFilterRequest("offerAmountRange", "contains", "1000", null, null)],
            null)));

        Assert.Equal("grid.filter.operator.invalid", ex.ErrorCode);
    }
}
