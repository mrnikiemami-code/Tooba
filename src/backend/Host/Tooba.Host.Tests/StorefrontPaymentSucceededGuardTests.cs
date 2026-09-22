using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P10-T004-R14 — Payment Succeeded is terminal for new initiation.
/// </summary>
public sealed class StorefrontPaymentSucceededGuardTests
{
    [Fact]
    public void Initiation_paths_share_succeeded_checkout_guard()
    {
        var root = FindRepoRoot();
        var directory = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Payment", "Tooba.Payment.Infrastructure", "Directories", "PaymentDirectory.cs"));
        var composer = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Modules", "Payment", "Tooba.Payment.Application", "Models", "StorefrontPaymentOrchestrator.cs"));
        var endpoints = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontEndpoints.cs"));
        var contracts = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Payment", "Tooba.Payment.Application", "Ports", "PaymentDirectoryPorts.cs"));

        Assert.Contains("HasSucceededPaymentForCheckoutAsync", contracts, StringComparison.Ordinal);
        Assert.Contains("HasSucceededPaymentForCheckoutAsync", directory, StringComparison.Ordinal);
        Assert.Contains("HasSucceededPaymentForCheckoutAsync", composer, StringComparison.Ordinal);
        Assert.Contains("AlreadySucceeded()", directory, StringComparison.Ordinal);
        Assert.Contains("payment.already_succeeded", endpoints, StringComparison.Ordinal);
        Assert.Contains("payment.already_succeeded", directory, StringComparison.Ordinal);
        Assert.Contains("CanInitiatePayment", File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontModels.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void Frontend_hides_pay_when_capability_forbids()
    {
        var root = FindRepoRoot();
        var handoff = File.ReadAllText(Path.Combine(
            root, "src", "frontend", "app", "payment", "storefront-payment-handoff.tsx"));
        var paymentApi = File.ReadAllText(Path.Combine(
            root, "src", "frontend", "app", "storefront", "storefront-payment-api.ts"));
        Assert.Contains("canInitiatePayment", handoff, StringComparison.Ordinal);
        Assert.Contains("payment.already_succeeded", paymentApi, StringComparison.Ordinal);
        Assert.Contains("if (!page?.checkoutId || !method || paid) return;", handoff, StringComparison.Ordinal);
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
