using Tooba.Host.Admin;
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
        Assert.Contains("PayableAmount", list);
        Assert.DoesNotContain("PaymentSecret", list);

        var detail = typeof(AdminOrderDetailPage).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SellerOrders", detail);
        Assert.Contains("PostalAddress", detail);
        Assert.DoesNotContain("ProductPrice", detail);
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
        Assert.Contains("_offers.Offers", source, StringComparison.Ordinal);
        Assert.Contains("_orders.Checkouts", source, StringComparison.Ordinal);
        Assert.Contains("_parties.Parties", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_catalog.Products.Join(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_offers.Offers.Join(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_orders.Checkouts.Join(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_parties.Parties.Join(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FromSql", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Product.Price", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Product.Stock", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_admin_product_handler_invokes_server_authorization()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "ProductWorkspaceEndpoints.cs"));
        // List/Create/Get/History/PatchTitle + publish/unpublish/archive/delete + media×8 + variants×2 + seo×3 + grid query + brand assign/options + additional category add/remove
        Assert.Equal(31, Count(source, "AdminPanelAccess.RequireAuthorizedAsync"));
        Assert.Contains("IAuthorizationGuard", source, StringComparison.Ordinal);
        Assert.Contains("ICurrentTenant", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_admin_media_dam_handler_invokes_server_authorization()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Media", "MediaEndpoints.cs"));
        // upload + list + get metadata
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
