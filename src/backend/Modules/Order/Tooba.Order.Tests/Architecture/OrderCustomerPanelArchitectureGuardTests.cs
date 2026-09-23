using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>TB-TMAR-ORDER-GOLDEN-001-R9 — Customer Order list/detail/retry ownership guards.</summary>
public sealed class OrderCustomerPanelArchitectureGuardTests
{
    private const string CustomerSlice = "/Customer/";

    [Fact]
    public void Customer_slice_uses_contracts_only_without_foreign_app_infra_domain_or_utcnow()
    {
        var violations = CustomerSources()
            .Where(x =>
                x.Text.Contains("Tooba.Payment.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Payment.Infrastructure", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Payment.Domain", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Catalog.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Catalog.Infrastructure", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Catalog.Domain", StringComparison.Ordinal)
                || x.Text.Contains("CatalogDbContext", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Party.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Party.Infrastructure", StringComparison.Ordinal)
                || x.Text.Contains("PartyDbContext", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Host", StringComparison.Ordinal)
                || x.Text.Contains("PlatformHttpException", StringComparison.Ordinal)
                || x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                || x.Text.Contains("ex.Message", StringComparison.Ordinal)
                || x.Text.Contains("Message.Contains", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "customer slice foreign/time leaks: " + string.Join("; ", violations));
    }

    [Fact]
    public void Order_endpoints_own_customer_order_routes_via_ISender()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "CustomerOrderEndpoints.cs"));
        Assert.Contains("/orders", endpoints, StringComparison.Ordinal);
        Assert.Contains("/orders/{checkoutId:guid}", endpoints, StringComparison.Ordinal);
        Assert.Contains("/orders/{checkoutId:guid}/retry-unpaid", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender sender", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory api", endpoints, StringComparison.Ordinal);
        Assert.Contains("ListCustomerOrdersQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetCustomerOrderDetailQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("RetryCustomerUnpaidOrderCommand", endpoints, StringComparison.Ordinal);
        Assert.Contains("IOrderCustomerAuthorizer", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", endpoints, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "OrderEndpointModule.cs"));
        Assert.Contains("CustomerOrderEndpoints.Map", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_no_longer_registers_customer_order_routes_or_retry_business()
    {
        var hostEndpoints = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Customer", "CustomerPanelEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/orders\"", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/orders/{checkoutId:guid}\"", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/orders/{checkoutId:guid}/retry-unpaid\"", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("RetryUnpaidAsync", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ListOrdersAsync", hostEndpoints, StringComparison.Ordinal);
        Assert.Contains("GetCustomerOrderDashboardSummaryQuery", hostEndpoints, StringComparison.Ordinal);

        var composer = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Customer", "CustomerPanelComposer.cs"));
        Assert.DoesNotContain("OrderDbContext", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("RetryUnpaidAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ListOrdersAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrderAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("IReservationCycleCoordinator", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureUnpaidRetryHold", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void CQRS_handlers_return_Result_and_enforce_PlacedByUserId_ownership()
    {
        var list = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Customer", "Queries", "ListCustomerOrders",
            "ListCustomerOrdersQuery.cs"));
        Assert.Contains("IRequestHandler", list, StringComparison.Ordinal);
        Assert.Contains("Result<IReadOnlyList<CustomerOrderListItem>>", list, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Customer", "Queries", "GetCustomerOrderDetail",
            "GetCustomerOrderDetailQuery.cs"));
        Assert.Contains("IRequestHandler", detail, StringComparison.Ordinal);
        Assert.Contains("Result<CustomerOrderDetailPage>", detail, StringComparison.Ordinal);

        var summary = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Customer", "Queries", "GetCustomerOrderDashboardSummary",
            "GetCustomerOrderDashboardSummaryQuery.cs"));
        Assert.Contains("IRequestHandler", summary, StringComparison.Ordinal);
        Assert.Contains("Result<CustomerOrderDashboardSummary>", summary, StringComparison.Ordinal);

        var retry = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Customer", "Commands", "RetryCustomerUnpaidOrder",
            "RetryCustomerUnpaidOrderCommand.cs"));
        Assert.Contains("IRequestHandler", retry, StringComparison.Ordinal);
        Assert.Contains("Result<CustomerOrderDetailPage>", retry, StringComparison.Ordinal);
        Assert.Contains("IReservationCycleCoordinator", retry, StringComparison.Ordinal);
        Assert.Contains("EnsureRetryAfterExpiryAsync", retry, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", retry, StringComparison.Ordinal);

        var store = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Infrastructure", "Customer", "CustomerOrderCheckoutStore.cs"));
        Assert.Contains("x.PlacedByUserId == actorUserId", store, StringComparison.Ordinal);
        Assert.Contains("CheckoutId == checkoutId && x.PlacedByUserId == actorUserId", store, StringComparison.Ordinal);
    }

    [Fact]
    public void R4_through_R8_host_removals_remain_intact()
    {
        var host = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        Assert.False(File.Exists(Path.Combine(host, "Storefront", "StorefrontCheckoutComposer.cs")));
        Assert.False(File.Exists(Path.Combine(host, "Storefront", "StorefrontPendingPaymentComposer.cs")));
        Assert.False(File.Exists(Path.Combine(host, "Storefront", "StorefrontShippingComposer.cs")));
        Assert.False(File.Exists(Path.Combine(host, "Storefront", "CheckoutAbuseGate.cs")));
        Assert.False(File.Exists(Path.Combine(host, "Admin", "OrderInventoryRecoveryComposer.cs")));
        Assert.False(File.Exists(Path.Combine(host, "Admin", "OrderSupplyComposer.cs")));
        Assert.False(File.Exists(Path.Combine(host, "ReservationCycleCoordinator.cs")));
        Assert.False(File.Exists(Path.Combine(host, "ReservationCyclePolicyResolver.cs")));
    }

    private static IReadOnlyList<(string Path, string Text)> CustomerSources() =>
        Directory.EnumerateFiles(OrderRoot(), "*.cs", SearchOption.AllDirectories)
            .Select(x => x.Replace('\\', '/'))
            .Where(x => !x.Contains("/obj/", StringComparison.Ordinal) && !x.Contains("/bin/", StringComparison.Ordinal))
            .Where(x => !x.Contains("/Tooba.Order.Tests/", StringComparison.Ordinal))
            .Where(x => x.Contains(CustomerSlice, StringComparison.Ordinal)
                || x.EndsWith("/CustomerOrderEndpoints.cs", StringComparison.Ordinal))
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
