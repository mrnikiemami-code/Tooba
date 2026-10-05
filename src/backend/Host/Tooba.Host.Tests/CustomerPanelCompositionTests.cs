using Tooba.CustomerProfile.Application.Models;
using Tooba.Order.Application.Customer.Models;
using Tooba.Order.Contracts.Customer;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// Customer-account panel contracts after Host/Customer full closure:
/// Order list/detail remain Order.Application; dashboard RecentOrders cross via Order.Contracts.
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

        var dto = typeof(CustomerOrderListItemDto).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.True(list.SetEquals(dto));
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
    public void Customer_account_presentation_is_module_owned_and_contracts_only()
    {
        var root = FindRepoRoot();
        var hostCustomer = Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Customer");
        Assert.Empty(Directory.Exists(hostCustomer)
            ? Directory.GetFiles(hostCustomer, "*.cs", SearchOption.AllDirectories)
            : []);

        var endpoints = File.ReadAllText(Path.Combine(
            root,
            "src",
            "backend",
            "Modules",
            "CustomerProfile",
            "Tooba.CustomerProfile.Endpoints",
            "Customer",
            "CustomerProfileEndpoints.cs"));
        Assert.Contains("MapPut(\"/profile\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("CustomerProfileErrorCodes.SessionRequired", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Application", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Wishlist.Application", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AddressBook.Application", endpoints, StringComparison.Ordinal);

        var dashboard = File.ReadAllText(Path.Combine(
            root,
            "src",
            "backend",
            "Modules",
            "CustomerProfile",
            "Tooba.CustomerProfile.Endpoints",
            "CustomerDashboard",
            "CustomerAccountDashboardEndpoints.cs"));
        Assert.Contains("GetCustomerAccountDashboardQuery", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("GetCustomerOrderDashboardSummaryQuery", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", dashboard, StringComparison.Ordinal);
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
