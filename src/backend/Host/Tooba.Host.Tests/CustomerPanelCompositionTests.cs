using Tooba.Host.Customer;
using Tooba.Order.Application.Customer.Models;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قفل قرارداد پنل مشتری پس از R9: مالکیت سفارش در Order CQRS؛ Host فقط ترکیب نازک داشبورد/پروفایل.
/// </summary>
public sealed class CustomerPanelCompositionTests
{
    [Fact]
    public void Order_contract_uses_checkout_and_snapshot_amounts()
    {
        var list = typeof(CustomerOrderListItem).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("CheckoutId", list);
        Assert.Contains("PayableAmount", list);
        Assert.Contains("PaymentState", list);
        Assert.Equal(typeof(int), typeof(CustomerOrderListItem).GetProperty("ItemCount")!.PropertyType);
        Assert.DoesNotContain("ProductPrice", list);
        Assert.DoesNotContain("ProductStock", list);

        var detail = typeof(CustomerOrderDetailPage).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SellerOrders", detail);
        Assert.Contains("PostalAddress", detail);
        Assert.Contains("ShippingMethodLabel", detail);
    }

    [Fact]
    public void Optional_capabilities_are_explicit_and_not_fake_collections()
    {
        var dashboard = new CustomerDashboardPage(
            Guid.NewGuid(),
            "مشتری",
            0,
            0,
            0,
            WishlistAvailable: false,
            WishlistCount: 0,
            AddressBookAvailable: false,
            AddressBookCount: 0,
            RecentOrders: []);
        Assert.False(dashboard.WishlistAvailable);
        Assert.False(dashboard.AddressBookAvailable);
        Assert.Empty(dashboard.RecentOrders);
    }

    [Fact]
    public void Order_CQRS_filters_by_authenticated_actor_without_cross_schema_join()
    {
        var store = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Order",
            "Tooba.Order.Infrastructure",
            "Customer",
            "CustomerOrderCheckoutStore.cs"));
        Assert.Contains("x.PlacedByUserId == actorUserId", store, StringComparison.Ordinal);
        Assert.DoesNotContain(".Join(", store, StringComparison.Ordinal);
        Assert.DoesNotContain("FromSql", store, StringComparison.OrdinalIgnoreCase);

        var composer = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Order",
            "Tooba.Order.Application",
            "Customer",
            "CustomerOrderComposer.cs"));
        Assert.Contains("orders.Sum(x => x.TotalItemCount)", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Lines.Sum(line => line.Quantity)", composer, StringComparison.Ordinal);
        Assert.Contains("GetLatestForCheckoutAsync", composer, StringComparison.Ordinal);
        Assert.Contains("SellerOrderStatus.Cancelled", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("PaymentState(sellerOrder.Status)", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Domain", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_composer_has_no_OrderDbContext_and_dashboard_uses_summary_query()
    {
        var hostComposer = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Customer",
            "CustomerPanelComposer.cs"));
        Assert.DoesNotContain("OrderDbContext", hostComposer, StringComparison.Ordinal);
        Assert.DoesNotContain("RetryUnpaidAsync", hostComposer, StringComparison.Ordinal);
        Assert.Contains("ComposeDashboardAsync", hostComposer, StringComparison.Ordinal);
        Assert.Contains("CustomerOrderDashboardSummary", hostComposer, StringComparison.Ordinal);

        var endpoints = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Customer",
            "CustomerPanelEndpoints.cs"));
        Assert.Contains("session.IsAuthenticated", endpoints, StringComparison.Ordinal);
        Assert.Contains("session.UserId", endpoints, StringComparison.Ordinal);
        Assert.Contains("environment.IsDevelopment()", endpoints, StringComparison.Ordinal);
        Assert.Contains("customer.session.required", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetCustomerOrderDashboardSummaryQuery", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/orders\"", endpoints, StringComparison.Ordinal);
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
