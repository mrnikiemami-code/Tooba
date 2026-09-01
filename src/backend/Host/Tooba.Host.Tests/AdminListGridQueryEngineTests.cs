using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Admin;
using Tooba.Host.Grid;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class AdminListGridQueryEngineTests
{
    [Fact]
    public void Bounded_policy_pages_and_filters_in_memory_for_tests_only()
    {
        var rows = new List<AdminOrderListItem>
        {
            new(Guid.NewGuid(), "ORD-1", DateTimeOffset.UtcNow, "Ali", 1, "فروشگاه آرمان", 2, 100m, "IRR", "Paid", "Paid"),
            new(Guid.NewGuid(), "ORD-2", DateTimeOffset.UtcNow.AddDays(-1), "Sara", 2, "2 فروشنده", 3, 200m, "IRR", "PendingPayment", "Submitted"),
        };

        var request = new GridQueryRequest(
            1,
            1,
            "Ali",
            [new GridSortRequest("reference", "asc")],
            [],
            null);

        var page = AdminListGridPolicies.Orders.Execute(rows, request);

        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("ORD-1", page.Items[0].Reference);
    }

    [Fact]
    public void Orders_policy_rejects_invalid_filter_field()
    {
        var request = new GridQueryRequest(
            1,
            20,
            null,
            [],
            [new GridFilterRequest("unknown", "contains", "x", null, null)],
            null);

        Assert.Throws<PlatformHttpException>(() => AdminListGridPolicies.Orders.Normalize(request));
    }
}
