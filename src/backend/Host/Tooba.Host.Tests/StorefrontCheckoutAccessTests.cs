using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P10-T004-R13 — post-commit checkout ownership is not checkout.rejected.
/// </summary>
public sealed class StorefrontCheckoutAccessTests
{
    [Fact]
    public void Checkout_get_maps_access_denied_not_order_registration_failed()
    {
        var root = FindRepoRoot();
        var endpoints = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontEndpoints.cs"));
        var checkout = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontCheckoutComposer.cs"));
        var cart = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontCartComposer.cs"));

        Assert.Contains("checkout.access.denied", endpoints, StringComparison.Ordinal);
        Assert.Contains("payment.access.denied", endpoints, StringComparison.Ordinal);
        Assert.Contains("TryGetForOwnershipAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("TryGetForOwnershipAsync", cart, StringComparison.Ordinal);
        Assert.Contains("snapshot.Status.ToString()", cart, StringComparison.Ordinal);
        Assert.DoesNotContain("current active Cart as post-commit ownership", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Frontend_separates_active_cart_from_committed_proof()
    {
        var root = FindRepoRoot();
        var cartApi = File.ReadAllText(Path.Combine(
            root, "src", "frontend", "app", "storefront", "storefront-cart-api.ts"));
        var paymentApi = File.ReadAllText(Path.Combine(
            root, "src", "frontend", "app", "storefront", "storefront-payment-api.ts"));
        var shippingApi = File.ReadAllText(Path.Combine(
            root, "src", "frontend", "app", "storefront", "storefront-shipping-api.ts"));

        Assert.Contains("tooba.storefront.committedCheckoutProofs", cartApi, StringComparison.Ordinal);
        Assert.Contains("resolveCommittedCheckoutAccess", cartApi, StringComparison.Ordinal);
        Assert.Contains("persistCommittedCheckoutAndDetachActiveCart", shippingApi, StringComparison.Ordinal);
        Assert.Contains("resolveCommittedCheckoutAccess", paymentApi, StringComparison.Ordinal);
        Assert.DoesNotContain("نشست سبد فقط بعد از Paid پاک می‌شود", cartApi, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
