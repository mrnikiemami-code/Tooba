using Tooba.BuildingBlocks.Grid;
using Tooba.Order.Application.Admin.OrdersGrid;
using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>
/// نگهبان معماری برش گرید سفارش‌های Admin: مالکیت Order، قرارداد-only بیگانه،
/// و نبود موتور پرس‌وجوی گرید در Host.
/// </summary>
public sealed class OrderOrdersGridArchitectureGuardTests
{
    private const string GridSlice = "/Admin/OrdersGrid/";

    [Fact]
    public void Grid_slice_never_references_foreign_application_or_dbcontext()
    {
        var violations = GridSources()
            .Where(x =>
                x.Text.Contains("Tooba.Party.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Party.Infrastructure", StringComparison.Ordinal)
                || x.Text.Contains("PartyDbContext", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Returns.Application", StringComparison.Ordinal)
                || x.Text.Contains("ReturnsDbContext", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Inventory.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Host", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "orders grid slice foreign leaks: " + string.Join("; ", violations));
    }

    [Fact]
    public void Grid_reader_resolves_seller_labels_through_party_contracts()
    {
        var reader = File.ReadAllText(ReaderPath());
        Assert.Contains("IPartyLookup", reader, StringComparison.Ordinal);
        Assert.Contains("GetDisplayNamesAsync", reader, StringComparison.Ordinal);
        Assert.Contains("FilterIdsByDisplayNameAsync", reader, StringComparison.Ordinal);
        Assert.Contains("IReturnAdminOperations", reader, StringComparison.Ordinal);
        Assert.Contains("EfGridQuery.PageAsync", reader, StringComparison.Ordinal);
        Assert.DoesNotContain(".Join(", reader, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_no_longer_owns_the_orders_grid_query_engine()
    {
        Assert.False(File.Exists(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Grid", "AdminOrdersGridQueryEngine.cs")));

        var composer = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "AdminPanelComposer.cs"));
        Assert.DoesNotContain("QueryOrdersGridAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminOrdersGridQueryEngine", composer, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.DoesNotContain("AdminOrdersGridQueryEngine", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Orders_grid_route_is_mapped_once_and_only_by_order_endpoints()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "Admin", "OrdersGrid", "AdminOrdersGridEndpoints.cs"));
        Assert.Contains("/v1/admin/orders/query", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender sender", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory api", endpoints, StringComparison.Ordinal);
        Assert.Contains("auth.RequireAdminAsync", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("IAdminOrdersGridReader", endpoints, StringComparison.Ordinal);

        var hostAdmin = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "AdminPanelEndpoints.cs"));
        Assert.DoesNotContain("MapPost(\"/orders/query\"", hostAdmin, StringComparison.Ordinal);
    }

    [Fact]
    public void Owning_modules_register_the_new_admin_operation_contract_adapters()
    {
        var expectations = new (string Module, string Path, string Needle)[]
        {
            ("Fulfillment", "Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs", "IFulfillmentAdminOperations"),
            ("Returns", "Tooba.Returns.Infrastructure/DependencyInjection/ReturnsModule.cs", "IReturnAdminOperations"),
            ("Settlement", "Tooba.Settlement.Infrastructure/DependencyInjection/SettlementModule.cs", "ISettlementOrderAccrualPort"),
        };
        foreach (var (module, path, needle) in expectations)
        {
            var file = Path.Combine(RepoRoot(), "src", "backend", "Modules", module, path.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(file), $"missing module registration file: {file}");
            Assert.Contains(needle, File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Policy_rejects_unknown_fields_and_defaults_to_created_desc()
    {
        var normalized = AdminOrdersGridPolicy.Normalize(new GridQueryRequest(0, 0, "  ", [], [], null));
        Assert.Equal("created", normalized.Sort[0].Field);
        Assert.Equal("desc", normalized.Sort[0].Direction);

        Assert.Throws<GridQueryValidationException>(() => AdminOrdersGridPolicy.Normalize(
            new GridQueryRequest(1, 20, null, [], [new GridFilterRequest("secret", "contains", "x", null, null)], null)));

        Assert.Throws<GridQueryValidationException>(() => AdminOrdersGridPolicy.Normalize(
            new GridQueryRequest(1, 20, null, [], [new GridFilterRequest("created", "contains", "x", null, null)], null)));
    }

    private static string ReaderPath() => Path.Combine(
        OrderRoot(), "Tooba.Order.Infrastructure", "Admin", "OrdersGrid", "AdminOrdersGridReader.cs");

    private static IReadOnlyList<(string Path, string Text)> GridSources() =>
        Directory.EnumerateFiles(OrderRoot(), "*.cs", SearchOption.AllDirectories)
            .Select(x => x.Replace('\\', '/'))
            .Where(x => !x.Contains("/obj/", StringComparison.Ordinal) && !x.Contains("/bin/", StringComparison.Ordinal))
            .Where(x => !x.Contains("/Tooba.Order.Tests/", StringComparison.Ordinal))
            .Where(x => x.Contains(GridSlice, StringComparison.Ordinal))
            .Select(x => (Path.GetRelativePath(RepoRoot(), x), File.ReadAllText(x)))
            .ToList();

    private static string OrderRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
