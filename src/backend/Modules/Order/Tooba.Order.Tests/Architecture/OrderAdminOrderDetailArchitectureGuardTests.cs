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

/// <summary>TB-TMAR-ORDER-GOLDEN-001-R6 — Admin Order Detail + AdminViewAck ownership guards.</summary>
public sealed class OrderAdminOrderDetailArchitectureGuardTests
{
    private const string DetailSlice = "/Admin/Detail/";

    [Fact]
    public void Detail_slice_never_references_foreign_application_infra_domain_or_dbcontext()
    {
        var violations = DetailSources()
            .Where(x =>
                x.Text.Contains("Tooba.Party.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Party.Infrastructure", StringComparison.Ordinal)
                || x.Text.Contains("PartyDbContext", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Catalog.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Catalog.Infrastructure", StringComparison.Ordinal)
                || x.Text.Contains("CatalogDbContext", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Fulfillment.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Fulfillment.Infrastructure", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Fulfillment.Domain", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Returns.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Returns.Domain", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Settlement.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Settlement.Domain", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Payment.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Host", StringComparison.Ordinal)
                || x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                || x.Text.Contains("ex.Message", StringComparison.Ordinal)
                || x.Text.Contains("Message.Contains", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "admin detail slice foreign/time leaks: " + string.Join("; ", violations));
    }

    [Fact]
    public void Order_endpoints_own_detail_route_via_ISender()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "AdminOrderDetailEndpoints.cs"));
        Assert.Contains("/v1/admin/orders/{checkoutId:guid}", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender sender", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory api", endpoints, StringComparison.Ordinal);
        Assert.Contains("auth.RequireAdminAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetAdminOrderDetailQuery", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("IAdminOrderDetailCheckoutStore", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", endpoints, StringComparison.Ordinal);

        var hostAdmin = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "AdminPanelEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/orders/{checkoutId:guid}\"", hostAdmin, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrderAsync", hostAdmin, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_composer_no_longer_owns_GetOrderAsync_or_AdminViewAck()
    {
        var composer = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "AdminPanelComposer.cs"));
        Assert.DoesNotContain("GetOrderAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminViewAcks", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("CheckoutAdminViewAck", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildFinancialEvents", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("LineOperationalStatus", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminViewAck_persisted_in_Order_with_IClock()
    {
        var store = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Infrastructure", "Admin", "Detail", "AdminOrderDetailCheckoutStore.cs"));
        Assert.Contains("AdminViewAcks", store, StringComparison.Ordinal);
        Assert.Contains("CheckoutAdminViewAck.Create", store, StringComparison.Ordinal);
        Assert.Contains("IClock", store, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", store, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime.UtcNow", store, StringComparison.Ordinal);

        var handler = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Admin", "Detail", "Queries",
            "GetAdminOrderDetail", "GetAdminOrderDetailQuery.cs"));
        Assert.Contains("IClock", handler, StringComparison.Ordinal);
        Assert.Contains("clock.UtcNow", handler, StringComparison.Ordinal);
        Assert.Contains("IRequestHandler", handler, StringComparison.Ordinal);
        Assert.Contains("Result<AdminOrderDetailPage>", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void R5_and_R4_host_removals_remain_intact()
    {
        var hostStorefront = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Storefront");
        Assert.False(File.Exists(Path.Combine(hostStorefront, "StorefrontCheckoutComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostStorefront, "StorefrontPendingPaymentComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostStorefront, "StorefrontShippingComposer.cs")));
        Assert.False(File.Exists(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Storefront", "CheckoutAbuseGate.cs")));
        Assert.False(File.Exists(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "OrderInventoryRecoveryComposer.cs")));
        Assert.False(File.Exists(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "OrderSupplyComposer.cs")));
    }

    [Fact]
    public void Settlement_registers_admin_order_detail_contract_adapter()
    {
        var module = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Modules", "Settlement",
            "Tooba.Settlement.Infrastructure", "DependencyInjection", "SettlementModule.cs"));
        Assert.Contains("ISettlementAdminOrderDetailReader", module, StringComparison.Ordinal);
    }

    private static IReadOnlyList<(string Path, string Text)> DetailSources() =>
        Directory.EnumerateFiles(OrderRoot(), "*.cs", SearchOption.AllDirectories)
            .Select(x => x.Replace('\\', '/'))
            .Where(x => !x.Contains("/obj/", StringComparison.Ordinal) && !x.Contains("/bin/", StringComparison.Ordinal))
            .Where(x => !x.Contains("/Tooba.Order.Tests/", StringComparison.Ordinal))
            .Where(x => x.Contains(DetailSlice, StringComparison.Ordinal))
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
