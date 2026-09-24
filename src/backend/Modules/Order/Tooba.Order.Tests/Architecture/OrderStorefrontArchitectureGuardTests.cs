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

/// <summary>TB-TMAR-ORDER-GOLDEN-001-R5 — Storefront Order ownership guards.</summary>
public sealed class OrderStorefrontArchitectureGuardTests
{
    [Fact]
    public void Host_storefront_order_composer_files_are_absent()
    {
        var hostStorefront = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Storefront");
        Assert.False(File.Exists(Path.Combine(hostStorefront, "StorefrontCheckoutComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostStorefront, "StorefrontPendingPaymentComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostStorefront, "StorefrontPendingPaymentProjector.cs")));
        Assert.False(File.Exists(Path.Combine(hostStorefront, "StorefrontShippingComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostStorefront, "StorefrontShippingCalculator.cs")));
    }

    [Fact]
    public void R4_host_recovery_supply_files_remain_absent()
    {
        var hostAdmin = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin");
        Assert.False(File.Exists(Path.Combine(hostAdmin, "OrderInventoryRecoveryComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "OrderSupplyComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "AdminOrderInventoryRecoverySupplyEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "HostAdminOrderOperationsInventoryRecoveryAdapter.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "HostAdminOrderOperationsSupplyAdapter.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "HostAdminOrderSupplyStatusReader.cs")));
    }

    [Fact]
    public void Host_storefront_endpoints_no_longer_own_migrated_routes()
    {
        var hostEndpoints = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontEndpoints.cs"));
        Assert.DoesNotContain("/checkout/preview", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/checkout\"", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("/pending-payments", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("hide-pending-card", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("/shipping/projection", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("/shipping/selection", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("/shipping/commit", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.StartsWith", hostEndpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_endpoints_own_nine_storefront_routes_via_ISender()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "Storefront", "StorefrontOrderEndpoints.cs"));
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("/pending-payments", endpoints, StringComparison.Ordinal);
        Assert.Contains("/checkout/{checkoutId:guid}/cancel", endpoints, StringComparison.Ordinal);
        Assert.Contains("hide-pending-card", endpoints, StringComparison.Ordinal);
        Assert.Contains("/checkout/preview", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/checkout\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/checkout/{checkoutId:guid}\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("/shipping/projection", endpoints, StringComparison.Ordinal);
        Assert.Contains("/shipping/selection", endpoints, StringComparison.Ordinal);
        Assert.Contains("/shipping/commit", endpoints, StringComparison.Ordinal);
        Assert.Contains("PreviewStorefrontCheckoutQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("SubmitStorefrontCheckoutCommand", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetStorefrontCheckoutQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("ListStorefrontPendingPaymentsQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("CancelPendingCheckoutCommand", endpoints, StringComparison.Ordinal);
        Assert.Contains("HidePendingPaymentCardCommand", endpoints, StringComparison.Ordinal);
        Assert.Contains("ProjectStorefrontShippingQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("SaveStorefrontShippingSelectionCommand", endpoints, StringComparison.Ordinal);
        Assert.Contains("CommitStorefrontShippingCommand", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Infrastructure", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Storefront_cqrs_folders_align_offer_style()
    {
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Checkout", "Queries",
            "PreviewStorefrontCheckout", "PreviewStorefrontCheckoutQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Checkout", "Commands",
            "SubmitStorefrontCheckout", "SubmitStorefrontCheckoutCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Checkout", "Queries",
            "GetStorefrontCheckout", "GetStorefrontCheckoutQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "PendingPayment", "Queries",
            "ListStorefrontPendingPayments", "ListStorefrontPendingPaymentsQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "PendingPayment", "Commands",
            "CancelPendingCheckout", "CancelPendingCheckoutCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "PendingPayment", "Commands",
            "HidePendingPaymentCard", "HidePendingPaymentCardCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Shipping", "Queries",
            "ProjectStorefrontShipping", "ProjectStorefrontShippingQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Shipping", "Commands",
            "SaveStorefrontShippingSelection", "SaveStorefrontShippingSelectionCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Shipping", "Commands",
            "CommitStorefrontShipping", "CommitStorefrontShippingCommand.cs")));
    }

    [Fact]
    public void Application_storefront_uses_contracts_only_no_foreign_application()
    {
        var storefrontRoot = Path.Combine(OrderRoot(), "Tooba.Order.Application", "Storefront");
        var sources = Directory.GetFiles(storefrontRoot, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(sources);
        foreach (var path in sources)
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("Tooba.Cart.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Cart.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Cart.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AddressBook.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AddressBook.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Catalog.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Catalog.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Payment.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Payment.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Settlement.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Settlement.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Fulfillment.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Fulfillment.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Fulfillment.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("OrderDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("CatalogDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DateTimeOffset.UtcNow", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DateTime.UtcNow", text, StringComparison.Ordinal);
        }

        var csproj = File.ReadAllText(Path.Combine(OrderRoot(), "Tooba.Order.Application", "Tooba.Order.Application.csproj"));
        Assert.DoesNotContain("Cart.Application", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("AddressBook.Application", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Catalog.Application", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Payment.Application", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Settlement.Application", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Fulfillment.Application", csproj, StringComparison.Ordinal);
        Assert.Contains("Fulfillment.Contracts", csproj, StringComparison.Ordinal);
        Assert.Contains("Cart.Contracts", csproj, StringComparison.Ordinal);
        Assert.Contains("AddressBook.Contracts", csproj, StringComparison.Ordinal);
        Assert.Contains("Payment.Contracts", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Storefront_has_no_central_code_switch_dispatcher()
    {
        var storefrontRoot = Path.Combine(OrderRoot(), "Tooba.Order.Application", "Storefront");
        var handlerFiles = Directory.GetFiles(storefrontRoot, "*Command.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(storefrontRoot, "*Query.cs", SearchOption.AllDirectories))
            .ToList();
        Assert.True(handlerFiles.Count >= 9, $"expected >=9 CQRS files, got {handlerFiles.Count}");

        foreach (var path in handlerFiles)
        {
            var text = File.ReadAllText(path);
            Assert.Contains("IRequestHandler", text, StringComparison.Ordinal);
            Assert.DoesNotContain("with { Code =", text, StringComparison.Ordinal);
            Assert.DoesNotContain("request.Code switch", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ExecuteCoreAsync", text, StringComparison.Ordinal);
        }

        // No mega dispatcher service with action-code switch
        var services = Directory.GetFiles(
            Path.Combine(storefrontRoot, "Services"), "*.cs", SearchOption.TopDirectoryOnly);
        foreach (var path in services)
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("\"preview\" => await", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\"submit\" => await", text, StringComparison.Ordinal);
            Assert.DoesNotContain("code switch", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Host_checkout_abuse_gate_is_absent()
    {
        Assert.False(File.Exists(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Storefront", "CheckoutAbuseGate.cs")));
        var program = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.DoesNotContain("CheckoutAbuseGate", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ICheckoutAbuseGate", program, StringComparison.Ordinal);
    }

    /// <summary>
    /// TB-TMAR-CART-MULTICURRENCY-LINES-001-R1 — Order boundary may not sum unlike currencies and may
    /// not derive Order/Checkout currency authority from Cart.DefaultCurrency.
    /// </summary>
    [Fact]
    public void Order_boundary_is_single_currency_fail_closed_and_never_uses_cart_default_currency()
    {
        var shipping = StripCommentLines(File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Services", "StorefrontShippingService.cs")));
        var checkout = StripCommentLines(File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Services", "StorefrontCheckoutService.cs")));
        var compatibility = StripCommentLines(File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Services", "StorefrontCartCurrencyCompatibility.cs")));
        var directory = StripCommentLines(File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Infrastructure", "Checkout", "Persistence", "CheckoutDirectory.cs")));
        var submitHost = StripCommentLines(File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Infrastructure", "Checkout", "Persistence", "CheckoutSubmitHost.cs")));
        var errors = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "StorefrontOrderErrors.cs"));

        // 1. No cross-currency subtotal arithmetic in Order shipping.
        Assert.DoesNotContain("TotalsByCurrency.Sum", shipping, StringComparison.Ordinal);
        Assert.DoesNotContain("TotalsByCurrency.Sum", compatibility, StringComparison.Ordinal);

        // 2. Cart.DefaultCurrency is never Order transaction/pricing/order currency authority.
        foreach (var text in new[] { shipping, checkout, directory, submitHost })
        {
            Assert.DoesNotContain("cart.DefaultCurrency", text, StringComparison.Ordinal);
        }

        // 3. Order pricing/order currency authority uses the sole line currency.
        Assert.Contains("ResolveSoleCurrency", shipping, StringComparison.Ordinal);
        Assert.Contains("ResolveSoleCurrency", checkout, StringComparison.Ordinal);
        Assert.Contains("ResolveSoleCurrency(cart)", directory, StringComparison.Ordinal);
        Assert.Contains("ResolveSoleCurrency(cart)", submitHost, StringComparison.Ordinal);

        // 4. Typed stable mixed-currency fail-closed error exists and is used by the helper only.
        Assert.Contains("checkout.multicurrency.not_supported", errors, StringComparison.Ordinal);
        Assert.Contains("CheckoutMultiCurrencyNotSupported", compatibility, StringComparison.Ordinal);
        Assert.DoesNotContain(".DefaultCurrency", compatibility, StringComparison.Ordinal);

        // 5. No Host business implementation added for this repair.
        Assert.False(Directory.Exists(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Storefront", "CurrencyCompatibility")));
    }

    [Fact]
    public void Order_owns_checkout_abuse_gate_with_IClock_and_catalog_contract()
    {
        var gatePath = Path.Combine(
            OrderRoot(), "Tooba.Order.Infrastructure", "Checkout", "Abuse", "CheckoutAbuseGate.cs");
        Assert.True(File.Exists(gatePath));
        var gate = File.ReadAllText(gatePath);
        Assert.Contains("IClock", gate, StringComparison.Ordinal);
        Assert.Contains("_clock.UtcNow", gate, StringComparison.Ordinal);
        Assert.Contains("IStoreCheckoutAbuseSettingsReader", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime.UtcNow", gate, StringComparison.Ordinal);
        Assert.Contains("OrderDbContext", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckoutProcessManager_has_no_message_based_inventory_conflict_classification()
    {
        var manager = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Checkout", "Process", "CheckoutProcessManager.cs"));
        Assert.DoesNotContain("ex.Message == \"inventory.reservation.conflict\"", manager, StringComparison.Ordinal);
        Assert.DoesNotContain("when (ex.Message", manager, StringComparison.Ordinal);
        Assert.Contains("ex.Code == \"inventory.reservation.conflict\"", manager, StringComparison.Ordinal);
    }

    [Fact]
    public void Shipping_and_pending_services_use_IClock()
    {
        var shipping = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Services", "StorefrontShippingService.cs"));
        Assert.Contains("IClock", shipping, StringComparison.Ordinal);
        Assert.Contains("_clock.UtcNow", shipping, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", shipping, StringComparison.Ordinal);

        var pending = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Services", "StorefrontPendingPaymentService.cs"));
        Assert.Contains("IClock", pending, StringComparison.Ordinal);
        Assert.Contains("_clock.UtcNow", pending, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", pending, StringComparison.Ordinal);

        var calculator = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Application", "Storefront", "Services", "StorefrontShippingCalculator.cs"));
        Assert.Contains("Fulfillment.Contracts", calculator, StringComparison.Ordinal);
        Assert.DoesNotContain("Fulfillment.Application", calculator, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    /// <summary>Drops comment lines so documentation may still explain the forbidden rule.</summary>
    private static string StripCommentLines(string source) =>
        string.Join(
            '\n',
            source.Split('\n').Where(line => !line.TrimStart().StartsWith("///", StringComparison.Ordinal)
                                             && !line.TrimStart().StartsWith("//", StringComparison.Ordinal)));

    private static string OrderRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order");
}
