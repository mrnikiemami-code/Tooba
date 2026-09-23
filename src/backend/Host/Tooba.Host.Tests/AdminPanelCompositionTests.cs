using Tooba.Host.Admin;
using Tooba.Order.Application.Admin.Customers.Models;
using Tooba.Order.Application.Admin.Detail.Models;
using Tooba.Order.Application.Admin.OrdersGrid.Models;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قفل قرارداد read model مدیر و جداسازی خواندن ماژول‌ها.
/// </summary>
public sealed class AdminPanelCompositionTests
{
    [Fact]
    public void Order_contract_uses_checkout_snapshots_and_safe_recipient_fields()
    {
        var list = typeof(AdminOrderListItem).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("CheckoutId", list);
        Assert.Contains("SellerCount", list);
        Assert.Contains("SellerDisplayNames", list);
        Assert.Contains("PayableAmount", list);
        Assert.DoesNotContain("PaymentSecret", list);

        var detail = typeof(AdminOrderDetailPage).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SellerOrders", detail);
        Assert.Contains("LineCount", detail);
        Assert.Contains("SellerCount", detail);
        Assert.Contains("SellerFinancials", detail);
        Assert.Contains("FinancialEvents", detail);
        Assert.Contains("FinancialSummary", detail);
        Assert.Contains("PostalAddress", detail);
        Assert.Contains("ConsolidatedPackages", detail);
        Assert.DoesNotContain("ProductPrice", detail);

        var seller = typeof(AdminSellerOrderView).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("FulfillmentId", seller);
        Assert.Contains("FulfillmentStatus", seller);
        Assert.Contains("Shipments", seller);
        var line = typeof(AdminOrderLineView).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("OrderLineId", line);
        Assert.Contains("QuantityShipped", line);
        var shipment = typeof(AdminShipmentView).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("ShipmentId", shipment);
        Assert.Contains("CarrierDisplayName", shipment);
        Assert.Contains("TrackingReference", shipment);
        Assert.Contains("ActivePackageNumber", shipment);
        Assert.Contains("PackageLockedReasonFa", shipment);
    }

    [Fact]
    public void Seller_and_customer_contracts_are_narrow_operational_views()
    {
        var seller = typeof(AdminSellerListItem).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("ActiveOffers", seller);
        Assert.Contains("OrderCount", seller);
        Assert.DoesNotContain("OnboardingWorkflow", seller);

        var customer = typeof(AdminCustomerListItem).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("OrderCount", customer);
        Assert.Contains("LastOrderAt", customer);
        Assert.DoesNotContain("CrmScore", customer);
    }

    [Fact]
    public void Composer_reads_module_contexts_separately_and_composes_in_memory()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "AdminPanelComposer.cs"));
        Assert.Contains("_catalog.Products", source, StringComparison.Ordinal);
        Assert.Contains("IOfferQueryGateway", source, StringComparison.Ordinal);
        Assert.Contains("CountActiveOffersAsync", source, StringComparison.Ordinal);
        Assert.Contains("GetAdminOrderDashboardMetricsQuery", source, StringComparison.Ordinal);
        Assert.Contains("ISellerOrderCountReader", source, StringComparison.Ordinal);
        Assert.Contains("_parties.Parties", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OfferDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_offers.Offers", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_catalog.Products.Join(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_parties.Parties.Join(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FromSql", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GetOrderAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminViewAcks", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_payments.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_fulfillment.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Product.Price", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Product.Stock", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerOrderStatus", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ListOrdersAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ListCustomersAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_admin_endpoints_no_longer_own_order_or_customer_routes()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "AdminPanelEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/orders\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/orders/{checkoutId:guid}\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/customers\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/customers/query\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrderAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ListOrdersAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ListCustomersAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_admin_product_handler_invokes_server_authorization()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "ProductWorkspaceEndpoints.cs"));
        Assert.Equal(32, Count(source, "AdminPanelAccess.RequireAuthorizedAsync"));
        Assert.Contains("IAuthorizationGuard", source, StringComparison.Ordinal);
        Assert.Contains("ICurrentTenant", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_admin_media_dam_handler_invokes_server_authorization()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Media", "MediaEndpoints.cs"));
        Assert.Equal(3, Count(source, "AdminPanelAccess.RequireAuthorizedAsync"));
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
