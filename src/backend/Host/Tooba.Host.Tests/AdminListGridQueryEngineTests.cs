using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Admin;
using Tooba.Host.Grid;
using Tooba.Order.Application.Admin.OrdersGrid.Models;
using Tooba.Payment.Endpoints.Admin;
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
    public void Host_admin_list_grid_policies_has_no_payments_policy()
    {
        var hostGrid = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Grid", "AdminListGridPolicies.cs"));
        Assert.DoesNotContain("Payments", hostGrid, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminReceiptListItem", hostGrid, StringComparison.Ordinal);
    }

    [Fact]
    public void Payment_owned_normalizer_rejects_invalid_filter_field()
    {
        var normalizer = new PaymentAdminGridQueryNormalizer();
        var request = new GridQueryRequest(
            1,
            20,
            null,
            [],
            [new GridFilterRequest("unknown", "contains", "x", null, null)],
            null);

        var ex = Assert.Throws<SemanticException>(() => normalizer.Normalize(request));
        Assert.Equal("grid.filter.field.invalid", ex.Error.Code);
    }

    [Fact]
    public void Payment_owned_normalizer_preserves_default_sort_and_paging()
    {
        var normalizer = new PaymentAdminGridQueryNormalizer();
        var request = new GridQueryRequest(0, 0, null, [], [], null);

        var normalized = normalizer.Normalize(request);

        Assert.Equal(1, normalized.Page);
        Assert.Equal(20, normalized.PageSize);
        var sort = Assert.Single(normalized.Sort);
        Assert.Equal("created", sort.Field);
        Assert.Equal("desc", sort.Direction);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
