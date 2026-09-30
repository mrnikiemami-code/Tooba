using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Order.Application.Admin.OrdersGrid;
using Tooba.Payment.Endpoints.Admin;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class AdminListGridQueryEngineTests
{
    [Fact]
    public void Order_owned_AdminOrdersGridPolicy_is_sole_orders_normalize_owner()
    {
        var request = new GridQueryRequest(0, 0, "  ", [], [], null);
        var normalized = AdminOrdersGridPolicy.Normalize(request);

        Assert.Equal(1, normalized.Page);
        Assert.Equal(20, normalized.PageSize);
        var sort = Assert.Single(normalized.Sort);
        Assert.Equal("created", sort.Field);
        Assert.Equal("desc", sort.Direction);
        Assert.True(string.IsNullOrEmpty(normalized.Search));
    }

    [Fact]
    public void Order_owned_AdminOrdersGridPolicy_rejects_unknown_filter_field()
    {
        var request = new GridQueryRequest(
            1,
            20,
            null,
            [],
            [new GridFilterRequest("unknown", "contains", "x", null, null)],
            null);

        var ex = Assert.Throws<GridQueryValidationException>(() => AdminOrdersGridPolicy.Normalize(request));
        Assert.Equal("grid.filter.field.invalid", ex.ErrorCode);
    }

    [Fact]
    public void Host_Grid_directory_is_absent()
    {
        var hostGrid = Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Grid");
        Assert.False(Directory.Exists(hostGrid));
        Assert.False(File.Exists(Path.Combine(hostGrid, "AdminListGridPolicies.cs")));
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
