using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>TB-TMAR-ORDER-GOLDEN-001-R11 — Admin Panel Order residual ownership guards.</summary>
public sealed class OrderAdminPanelResidualArchitectureGuardTests
{
    private static readonly string[] R11Slices =
    [
        "/Admin/LegacyList/",
        "/Admin/Customers/",
        "/Admin/Dashboard/",
        "/Admin/Sellers/",
    ];

    [Fact]
    public void R11_slice_uses_contracts_only_without_foreign_app_infra_or_utcnow()
    {
        var violations = R11Sources()
            .Where(x =>
                x.Text.Contains("Tooba.Party.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Party.Infrastructure", StringComparison.Ordinal)
                || x.Text.Contains("PartyDbContext", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Offer.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Offer.Infrastructure", StringComparison.Ordinal)
                || x.Text.Contains("OfferDbContext", StringComparison.Ordinal)
                || x.Text.Contains("CatalogDbContext", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Host", StringComparison.Ordinal)
                || x.Text.Contains("PlatformHttpException", StringComparison.Ordinal)
                || x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                || x.Text.Contains("ex.Message", StringComparison.Ordinal)
                || x.Text.Contains("Message.Contains", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "R11 slice foreign/time leaks: " + string.Join("; ", violations));
    }

    [Fact]
    public void Order_endpoints_own_admin_orders_list_and_customers_via_ISender()
    {
        var orders = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "AdminOrdersGridEndpoints.cs"));
        Assert.Contains("MapGet(\"/v1/admin/orders\"", orders, StringComparison.Ordinal);
        Assert.Contains("ListAdminOrdersQuery", orders, StringComparison.Ordinal);
        Assert.Contains("ISender sender", orders, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory api", orders, StringComparison.Ordinal);
        Assert.Contains("IOrderAdminAuthorizer", orders, StringComparison.Ordinal);

        var customers = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "AdminCustomersEndpoints.cs"));
        Assert.Contains("MapGet(\"/v1/admin/customers\"", customers, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/v1/admin/customers/query\"", customers, StringComparison.Ordinal);
        Assert.Contains("ListAdminCustomersQuery", customers, StringComparison.Ordinal);
        Assert.Contains("QueryAdminCustomersGridQuery", customers, StringComparison.Ordinal);
        Assert.Contains("ISender sender", customers, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory api", customers, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "OrderEndpointModule.cs"));
        Assert.Contains("AdminCustomersEndpoints.Map", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_no_longer_owns_admin_orders_customers_or_OrderDbContext_in_admin_composers()
    {
        var host = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var endpoints = File.ReadAllText(Path.Combine(host, "Admin", "AdminPanelEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/orders\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/customers\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/customers/query\"", endpoints, StringComparison.Ordinal);

        var composer = File.ReadAllText(Path.Combine(host, "Admin", "AdminPanelComposer.cs"));
        Assert.DoesNotContain("OrderDbContext", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerOrderStatus", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ListOrdersAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ListCustomersAsync", composer, StringComparison.Ordinal);
        Assert.Contains("GetAdminOrderDashboardMetricsQuery", composer, StringComparison.Ordinal);
        Assert.Contains("ISellerOrderCountReader", composer, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(host, "Grid", "AdminCustomersGridQueryEngine.cs")));
        Assert.False(File.Exists(Path.Combine(host, "Admin", "AdminReservationCycleMapper.cs")));

        var sellers = File.ReadAllText(Path.Combine(host, "Grid", "AdminSellersGridQueryEngine.cs"));
        Assert.DoesNotContain("OrderDbContext", sellers, StringComparison.Ordinal);
        Assert.Contains("ISellerOrderCountReader", sellers, StringComparison.Ordinal);
    }

    [Fact]
    public void CQRS_handlers_and_customers_grid_are_Order_owned()
    {
        var listOrders = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "LegacyList", "Queries", "ListAdminOrders",
            "ListAdminOrdersQuery.cs"));
        Assert.Contains("IRequestHandler", listOrders, StringComparison.Ordinal);
        Assert.Contains("AdminOrdersGridProjection.MapOrderListItem", listOrders, StringComparison.Ordinal);

        var listCustomers = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Customers", "Queries", "ListAdminCustomers",
            "ListAdminCustomersQuery.cs"));
        Assert.Contains("IRequestHandler", listCustomers, StringComparison.Ordinal);

        var grid = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Customers", "Queries", "QueryAdminCustomersGrid",
            "QueryAdminCustomersGridQuery.cs"));
        Assert.Contains("AdminCustomersGridPolicy.Normalize", grid, StringComparison.Ordinal);

        var metrics = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Dashboard", "Queries",
            "GetAdminOrderDashboardMetrics", "GetAdminOrderDashboardMetricsQuery.cs"));
        Assert.Contains("IRequestHandler", metrics, StringComparison.Ordinal);

        var counts = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Sellers", "Queries", "GetSellerOrderCounts",
            "GetSellerOrderCountsQuery.cs"));
        Assert.Contains("IRequestHandler", counts, StringComparison.Ordinal);

        var customersInfra = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Infrastructure", "Admin", "Customers", "AdminCustomersGridReader.cs"));
        Assert.Contains("GroupBy(x => x.PlacedByUserId)", customersInfra, StringComparison.Ordinal);
        Assert.Contains("CountAsync", customersInfra, StringComparison.Ordinal);
        Assert.Contains("Skip(", customersInfra, StringComparison.Ordinal);
        Assert.Contains("Take(", customersInfra, StringComparison.Ordinal);
    }

    [Fact]
    public void R4_through_R10_host_removals_remain_intact()
    {
        var host = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        Assert.False(File.Exists(Path.Combine(host, "Storefront", "StorefrontCheckoutComposer.cs")));
        Assert.False(File.Exists(Path.Combine(host, "Storefront", "StorefrontPendingPaymentComposer.cs")));
        Assert.False(File.Exists(Path.Combine(host, "ReservationCycleCoordinator.cs")));
        Assert.False(File.Exists(Path.Combine(host, "ReservationCyclePolicyResolver.cs")));
        Assert.False(File.Exists(Path.Combine(host, "Grid", "AdminCustomersGridQueryEngine.cs")));

        var customer = File.ReadAllText(Path.Combine(host, "Customer", "CustomerPanelComposer.cs"));
        Assert.DoesNotContain("OrderDbContext", customer, StringComparison.Ordinal);
        var seller = File.ReadAllText(Path.Combine(host, "Seller", "SellerPanelComposer.cs"));
        Assert.DoesNotContain("OrderDbContext", seller, StringComparison.Ordinal);
    }

    private static IEnumerable<(string Path, string Text)> R11Sources()
    {
        foreach (var root in new[]
                 {
                     Path.Combine(OrderRoot(), "Tooba.Order.Application"),
                     Path.Combine(OrderRoot(), "Tooba.Order.Infrastructure"),
                     Path.Combine(OrderRoot(), "Tooba.Order.Endpoints"),
                 })
        {
            foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var normalized = path.Replace('\\', '/');
                if (!R11Slices.Any(s => normalized.Contains(s, StringComparison.Ordinal))
                    && !normalized.EndsWith("/AdminCustomersEndpoints.cs", StringComparison.Ordinal)
                    && !normalized.Contains("/AdminOrdersGridEndpoints.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return (normalized, File.ReadAllText(path));
            }
        }
    }

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
