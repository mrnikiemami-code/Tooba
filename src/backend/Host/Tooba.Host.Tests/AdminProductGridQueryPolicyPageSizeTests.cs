using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Grid;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class AdminProductGridQueryPolicyPageSizeTests
{
    [Fact]
    public void Normalize_AcceptsPageSize1000()
    {
        var normalized = AdminProductGridQueryPolicy.Normalize(new GridQueryRequest(1, 1000, null, [], [], null));
        Assert.Equal(1000, normalized.PageSize);
    }

    [Fact]
    public void Normalize_ClampsPageSizeAboveMaxTo1000()
    {
        var normalized = AdminProductGridQueryPolicy.Normalize(new GridQueryRequest(1, 1001, null, [], [], null));
        Assert.Equal(1000, normalized.PageSize);
    }

    [Fact]
    public void Normalize_DefaultsInvalidPageSizeToDefault()
    {
        var normalized = AdminProductGridQueryPolicy.Normalize(new GridQueryRequest(1, 0, null, [], [], null));
        Assert.Equal(AdminProductGridQueryPolicy.DefaultPageSize, normalized.PageSize);
    }
}
