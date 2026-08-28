using Tooba.BuildingBlocks.Grid;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class GridQueryPolicyBaseTests
{
    [Fact]
    public void NormalizePaging_ClampsPageSizeToMax()
    {
        var (page, pageSize) = GridQueryPolicyBase.NormalizePaging(0, 1001, 1000, 20);
        Assert.Equal(1, page);
        Assert.Equal(1000, pageSize);
    }

    [Fact]
    public void NormalizeSearch_TrimsAndCapsLength()
    {
        var search = GridQueryPolicyBase.NormalizeSearch($"  {"x".PadRight(250, 'a')}  ");
        Assert.NotNull(search);
        Assert.Equal(200, search!.Length);
    }

    [Fact]
    public void ValidateAdvancedConnectors_RejectsMismatch()
    {
        var ex = Assert.Throws<GridQueryValidationException>(() =>
            GridQueryPolicyBase.ValidateAdvancedConnectors(1, ["and"]));
        Assert.Equal("grid.advancedFilter.connector.count", ex.ErrorCode);
    }
}
