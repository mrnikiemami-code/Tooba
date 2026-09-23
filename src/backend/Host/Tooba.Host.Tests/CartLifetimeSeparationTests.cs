using Tooba.Cart.Application;
using Tooba.Cart.Contracts;
using Tooba.Order.Application;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;
using Tooba.Payment.Infrastructure.Adapters;
using Tooba.Payment.Infrastructure.DependencyInjection;
using Tooba.Payment.Infrastructure.Directories;
using Tooba.Payment.Infrastructure.Messaging;
using Tooba.Payment.Infrastructure.Providers;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// جداسازی ماندگاری سبد از رزرو سخت موجودی در مرز commit سفارش.
/// </summary>
public sealed class CartLifetimeSeparationTests
{
    [Fact]
    public void Cart_add_does_not_call_ReserveAsync()
    {
        var cartDir = Read("src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Directories/CartDirectory.cs");
        Assert.DoesNotContain("ReserveAsync", cartDir, StringComparison.Ordinal);
        Assert.Contains("EnsureSellableAsync", cartDir, StringComparison.Ordinal);
        Assert.Contains("_persistenceTtl", cartDir, StringComparison.Ordinal);
        Assert.Contains("GetAvailabilityBatchAsync", cartDir, StringComparison.Ordinal);
    }

    [Fact]
    public void Checkout_submit_creates_order_hold_preview_does_not()
    {
        var checkout = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs");
        var adapter = Read("src/backend/Modules/Inventory/Tooba.Inventory.Application/Checkout/CheckoutInventoryReservationAdapter.cs");
        Assert.Contains("ReserveCartLinesForOrderAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("requireReservation: false", checkout, StringComparison.Ordinal);
        Assert.Contains("order-commit:", adapter, StringComparison.Ordinal);
        Assert.Contains("inventory.supply.unavailable", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("ReserveAsync(", Read("src/backend/Modules/Cart/Tooba.Cart.Application/Presentation/CartPresentationComposer.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_separate_cart_persistence_from_payment_holds()
    {
        var app = Read("src/backend/Host/Tooba.Host/appsettings.json");
        Assert.Contains("\"PersistenceHours\": 168", app, StringComparison.Ordinal);
        Assert.Contains("OnlinePaymentHoldHours", app, StringComparison.Ordinal);
        Assert.Contains("ManualPaymentInitialHoldHours", app, StringComparison.Ordinal);
        Assert.Contains("ManualPaymentReviewHoldHours", app, StringComparison.Ordinal);
        Assert.Contains("CartLifetimeOptions", Read("src/backend/Modules/Cart/Tooba.Cart.Application/Lifetime/CartLifetimeOptions.cs"), StringComparison.Ordinal);
        Assert.Contains(nameof(ICheckoutReservationHoldPolicy), typeof(ICheckoutReservationHoldPolicy).Name);
        var policy = new CheckoutReservationHoldPolicy(
            Microsoft.Extensions.Options.Options.Create(new PaymentGatewayOptions
            {
                OnlinePaymentHoldHours = 2,
                ManualPaymentInitialHoldHours = 3,
            }));
        var now = DateTimeOffset.Parse("2026-09-12T00:00:00Z");
        Assert.Equal(now.AddHours(3), policy.ResolveInitialExpiresAt(now));
    }

    [Fact]
    public void Cart_availability_is_not_order_supply_status()
    {
        Assert.Equal(0, (int)CartLineAvailabilityKind.Available);
        Assert.Equal(1, (int)CartLineAvailabilityKind.LimitedQuantity);
        Assert.Equal(2, (int)CartLineAvailabilityKind.Unavailable);
        var contracts = Read("src/backend/Modules/Cart/Tooba.Cart.Contracts/Checkout/CartContracts.cs");
        Assert.Contains("CartLineAvailabilityKind", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderSupplyStatusKind", contracts, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_registers_hold_policy_and_cart_lifetime()
    {
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("ICheckoutReservationHoldPolicy", program, StringComparison.Ordinal);
        Assert.Contains("CartLifetimeOptions", program, StringComparison.Ordinal);
    }

    private static string Read(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(relative);
    }
}
