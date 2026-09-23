using Xunit;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Tests.Architecture;

/// <summary>TB-TMAR-ORDER-GOLDEN-001-R10 — Seller Order list/detail/dashboard ownership guards.</summary>
public sealed class OrderSellerPanelArchitectureGuardTests
{
    private const string SellerSlice = "/Seller/";

    [Fact]
    public void Seller_slice_uses_contracts_only_without_foreign_app_infra_domain_or_utcnow()
    {
        var violations = SellerSources()
            .Where(x =>
                x.Text.Contains("Tooba.AccessControl.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.AccessControl.Infrastructure", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.AccessControl.Domain", StringComparison.Ordinal)
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
        Assert.True(violations.Count == 0, "seller slice foreign/time leaks: " + string.Join("; ", violations));
    }

    [Fact]
    public void Order_endpoints_own_seller_order_routes_via_ISender()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "Seller", "SellerOrderEndpoints.cs"));
        Assert.Contains("/orders", endpoints, StringComparison.Ordinal);
        Assert.Contains("/orders/{sellerOrderId:guid}", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender sender", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory api", endpoints, StringComparison.Ordinal);
        Assert.Contains("ListSellerOrdersQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetSellerOrderDetailQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("IOrderSellerAuthorizer", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", endpoints, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "OrderEndpointModule.cs"));
        Assert.Contains("SellerOrderEndpoints.Map", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_no_longer_registers_seller_order_routes_or_OrderDbContext()
    {
        var hostEndpoints = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller", "SellerPanelEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/orders\"", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/orders/{sellerOrderId:guid}\"", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ListOrdersAsync", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrderAsync", hostEndpoints, StringComparison.Ordinal);
        Assert.Contains("GetSellerOrderDashboardSummaryQuery", hostEndpoints, StringComparison.Ordinal);

        var composer = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller", "SellerPanelComposer.cs"));
        Assert.DoesNotContain("OrderDbContext", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ListOrdersAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrderAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveOrderViewScopeAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerOrderStatus", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("IAccessControlDirectory", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void CQRS_handlers_return_Result_and_scope_filtering_lives_in_Order()
    {
        var list = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Seller", "Queries", "ListSellerOrders",
            "ListSellerOrdersQuery.cs"));
        Assert.Contains("IRequestHandler", list, StringComparison.Ordinal);
        Assert.Contains("Result<IReadOnlyList<SellerOrderListItem>>", list, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Seller", "Queries", "GetSellerOrderDetail",
            "GetSellerOrderDetailQuery.cs"));
        Assert.Contains("IRequestHandler", detail, StringComparison.Ordinal);
        Assert.Contains("Result<SellerOrderDetailPage>", detail, StringComparison.Ordinal);

        var summary = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Seller", "Queries", "GetSellerOrderDashboardSummary",
            "GetSellerOrderDashboardSummaryQuery.cs"));
        Assert.Contains("IRequestHandler", summary, StringComparison.Ordinal);
        Assert.Contains("Result<SellerOrderDashboardSummary>", summary, StringComparison.Ordinal);

        var composer = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Seller", "SellerOrderComposer.cs"));
        Assert.Contains("ISellerOrderViewAccessReader", composer, StringComparison.Ordinal);
        Assert.Contains("IsAllowed", composer, StringComparison.Ordinal);
        Assert.Contains("ICatalogVariantLookup", composer, StringComparison.Ordinal);
        Assert.Contains("IPartyLookup", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("IAccessControlDirectory", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogLookupGateway", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("IPartyLookupGateway", composer, StringComparison.Ordinal);

        var store = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Infrastructure", "Seller", "SellerOrderStore.cs"));
        Assert.Contains("x.SellerPartyId == sellerPartyId", store, StringComparison.Ordinal);
        Assert.Contains("SellerOrderId == sellerOrderId && x.SellerPartyId == sellerPartyId", store, StringComparison.Ordinal);
    }

    [Fact]
    public void R4_through_R9_host_removals_remain_intact()
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

        var customerComposer = File.ReadAllText(Path.Combine(host, "Customer", "CustomerPanelComposer.cs"));
        Assert.DoesNotContain("OrderDbContext", customerComposer, StringComparison.Ordinal);
        var customerEndpoints = File.ReadAllText(Path.Combine(host, "Customer", "CustomerPanelEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/orders\"", customerEndpoints, StringComparison.Ordinal);
    }

    private static IReadOnlyList<(string Path, string Text)> SellerSources() =>
        Directory.EnumerateFiles(OrderRoot(), "*.cs", SearchOption.AllDirectories)
            .Select(x => x.Replace('\\', '/'))
            .Where(x => !x.Contains("/obj/", StringComparison.Ordinal) && !x.Contains("/bin/", StringComparison.Ordinal))
            .Where(x => !x.Contains("/Tooba.Order.Tests/", StringComparison.Ordinal))
            .Where(x => x.Contains(SellerSlice, StringComparison.Ordinal)
                || x.EndsWith("/SellerOrderEndpoints.cs", StringComparison.Ordinal))
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
