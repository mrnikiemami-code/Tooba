using Tooba.Order.Infrastructure;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T004-R21-R1 — مرز اتمی commit و سد تزریق خطا.</summary>
public sealed class AtomicCheckoutCommitTests
{
    [Fact]
    public void Submit_uses_one_ambient_transaction_and_convert_before_complete()
    {
        var root = FindRepoRoot();
        var checkout = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "CheckoutProcessManager.cs"));
        var host = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "CheckoutSubmitHost.cs"));
        var directory = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "CheckoutDirectory.cs"));
        var payment = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontPaymentComposer.cs"));
        var feCheckout = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "storefront", "storefront-checkout-api.ts"));
        var feShipping = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "storefront", "storefront-shipping-api.ts"));
        Assert.Contains("TransactionScope", checkout, StringComparison.Ordinal);
        Assert.Contains("OnAfterReserveAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("OnAfterOrderWriteAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("OnAfterCartConvertedWriteAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("scope.Complete()", checkout, StringComparison.Ordinal);
        Assert.Contains("ConvertCartAsync", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("ReconcileCartConversionAsync(group, command", checkout, StringComparison.Ordinal);
        Assert.Contains("ReserveForCheckoutAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("ReserveCartLinesForOrderAsync", directory, StringComparison.Ordinal);
        Assert.Contains("EnsureCanStartInitialReservationAsync", checkout, StringComparison.Ordinal);
        Assert.True(
            checkout.IndexOf("EnsureCanStartInitialReservationAsync", StringComparison.Ordinal)
            < checkout.IndexOf("ReserveForCheckoutAsync", StringComparison.Ordinal));
        Assert.Contains("PrepareInitialCommit", host, StringComparison.Ordinal);
        Assert.Contains("PrepareInitialCycleAsync", host, StringComparison.Ordinal);
        Assert.DoesNotContain("_checkouts.SubmitAsync", payment, StringComparison.Ordinal);
        Assert.Contains("InitiateAsync", payment, StringComparison.Ordinal);
        Assert.Contains("if (!response.ok)", feCheckout, StringComparison.Ordinal);
        Assert.Contains("persistCommittedCheckoutAndDetachActiveCart(page.checkoutId, page.cartId)", feCheckout, StringComparison.Ordinal);
        Assert.True(
            feCheckout.IndexOf("async function parseCheckout", StringComparison.Ordinal)
            < feCheckout.LastIndexOf("persistCommittedCheckoutAndDetachActiveCart(page.checkoutId, page.cartId)", StringComparison.Ordinal));
        Assert.True(
            feShipping.IndexOf("if (!response.ok)", StringComparison.Ordinal)
            < feShipping.LastIndexOf("persistCommittedCheckoutAndDetachActiveCart", StringComparison.Ordinal));
    }

    [Fact]
    public void Hide_and_cancel_locks_are_distinct()
    {
        var root = FindRepoRoot();
        var locks = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-LOCKS.md"));
        var composer = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontPendingPaymentComposer.cs"));
        Assert.Contains("LOCK-SF-103", locks, StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-104", locks, StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-105", locks, StringComparison.Ordinal);
        Assert.Contains("HidePendingCardAsync", composer, StringComparison.Ordinal);
        Assert.Contains("CancelSellerOrderAsync", composer, StringComparison.Ordinal);
        Assert.Contains("pending.hide.active_hold", composer, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
