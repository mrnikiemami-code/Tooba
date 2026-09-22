using Tooba.BuildingBlocks.Grid;
using Tooba.Returns.Application.Queries.QueryAdminReturnsGrid;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

public sealed class ReturnsErrorAndGridTests
{
    [Fact]
    public void Admin_return_grid_policy_defaults_createdAt_desc()
    {
        var normalized = AdminReturnGridQueryPolicy.Instance.Normalize(
            new GridQueryRequest(1, 20, null, null, null, null));
        Assert.Single(normalized.Sort!);
        Assert.Equal("createdAt", normalized.Sort![0].Field);
        Assert.Equal("desc", normalized.Sort![0].Direction);
    }

    [Fact]
    public void Admin_return_grid_policy_rejects_unknown_filter_field()
    {
        Assert.Throws<GridQueryValidationException>(() =>
            AdminReturnGridQueryPolicy.Instance.Normalize(
                new GridQueryRequest(1, 20, null, null,
                    [new GridFilterRequest("notAField", "eq", "x", null, null)], null)));
    }
}
