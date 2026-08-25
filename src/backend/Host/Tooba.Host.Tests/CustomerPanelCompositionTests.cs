using Tooba.Host.Customer;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قفل قرارداد پنل مشتری: مالکیت سفارش در Host و داده‌ها snapshot هستند.
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
            AddressBookAvailable: false,
            RecentOrders: []);
        Assert.False(dashboard.WishlistAvailable);
        Assert.False(dashboard.AddressBookAvailable);
        Assert.Empty(dashboard.RecentOrders);
    }

    [Fact]
    public void Composer_filters_orders_by_authenticated_actor_without_cross_schema_join()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Customer",
            "CustomerPanelComposer.cs"));
        Assert.Contains("x.PlacedByUserId == actorUserId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_orders.Checkouts.Join(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FromSql", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Product.Price", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Product.Stock", source, StringComparison.Ordinal);
        Assert.Contains("GetLatestForCheckoutAsync", source, StringComparison.Ordinal);
        Assert.Contains("PaymentStatus.Failed", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PaymentState(sellerOrder.Status)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Endpoints_prefer_existing_authenticated_session()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Customer",
            "CustomerPanelEndpoints.cs"));
        Assert.Contains("session.IsAuthenticated", source, StringComparison.Ordinal);
        Assert.Contains("session.UserId", source, StringComparison.Ordinal);
        Assert.Contains("environment.IsDevelopment()", source, StringComparison.Ordinal);
        Assert.Contains("customer.session.required", source, StringComparison.Ordinal);
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
