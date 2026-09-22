using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P10-T004-R4 — payment result ownership survives Cart finalization.
/// </summary>
public sealed class StorefrontPaymentResultOwnershipTests
{
    [Fact]
    public void Payment_and_checkout_composers_expose_owned_payment_result_path()
    {
        var root = FindRepoRoot();
        var payment = File.ReadAllText(Path.Combine(
            root,
            "src", "backend", "Modules", "Payment", "Tooba.Payment.Application", "Orchestration", "StorefrontPaymentOrchestrator.cs"));
        var checkout = File.ReadAllText(Path.Combine(
            root,
            "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "Services", "StorefrontCheckoutService.cs"));

        Assert.Contains("GetOwnedForPaymentResultAsync", payment, StringComparison.Ordinal);
        Assert.Contains("GetOwnedForPaymentResultAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("_ = cartId;", payment, StringComparison.Ordinal);
        Assert.Contains("snapshot.CartId", checkout, StringComparison.Ordinal);
        Assert.Contains("_actor", checkout, StringComparison.Ordinal);
        Assert.Contains("IsAuthenticated", File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Order", "HostOrderStorefrontActor.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain("new empty Cart authorizing old Payment", payment, StringComparison.Ordinal);
    }

    [Fact]
    public void Frontend_payment_result_uses_state_aware_polling_and_proof()
    {
        var root = FindRepoRoot();
        var resultUi = File.ReadAllText(Path.Combine(
            root,
            "src", "frontend", "app", "payment", "result", "storefront-payment-result.tsx"));
        var paymentApi = File.ReadAllText(Path.Combine(
            root,
            "src", "frontend", "app", "storefront", "storefront-payment-api.ts"));
        var cartApi = File.ReadAllText(Path.Combine(
            root,
            "src", "frontend", "app", "storefront", "storefront-cart-api.ts"));

        Assert.Contains("shouldPollStorefrontPayment", resultUi, StringComparison.Ordinal);
        Assert.Contains("inFlight", resultUi, StringComparison.Ordinal);
        Assert.Contains("writePaymentResultProof", resultUi, StringComparison.Ordinal);
        Assert.Contains("resolvePaymentResultAccess", paymentApi, StringComparison.Ordinal);
        Assert.Contains("tooba.storefront.paymentResultProof", cartApi, StringComparison.Ordinal);
        Assert.DoesNotContain("window.setInterval(() => void refresh(), 1500)", resultUi, StringComparison.Ordinal);
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
