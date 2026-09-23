using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Models;
using Tooba.AddressBook.Contracts;
using Tooba.Cart.Application.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Order.Application;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Domain;
using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Domain.ValueObjects;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T004-R16 — تصویر و قابلیت در انتظار پرداخت مشتری.</summary>
public sealed class StorefrontPendingPaymentTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-13T10:00:00Z");

    [Fact]
    public void Active_pending_order_is_returned_with_pay_action()
    {
        var checkout = Guid.NewGuid();
        var page = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout, "SO-ACTIVE")],
            Pay(checkout, "Pending", "sandbox"),
            Cycle(checkout, 1, ReservationCycleStatus.Active, 570, expires: Now.AddMinutes(9.5)),
            Now);
        var item = Assert.Single(page.Items);
        Assert.Equal("SO-ACTIVE", item.OrderReference);
        Assert.Equal("pay", item.PrimaryAction);
        Assert.True(item.CanInitiatePayment);
        Assert.False(item.CanRetryPayment);
        Assert.Equal("held", item.ReservationPresentation);
        Assert.Equal(570, item.SecondsRemaining);
        Assert.Equal(Now.AddMinutes(9.5), item.HoldEndsAt);
    }

    [Fact]
    public void Succeeded_payment_is_excluded()
    {
        var checkout = Guid.NewGuid();
        var page = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout, "SO-PAID")],
            Pay(checkout, "Succeeded", "sandbox"),
            Cycle(checkout, 1, ReservationCycleStatus.CommittedPaid, 0),
            Now);
        Assert.Empty(page.Items);
    }

    [Fact]
    public void Manual_awaiting_admin_is_informational()
    {
        var checkout = Guid.NewGuid();
        var page = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout, "SO-MANUAL")],
            new Dictionary<Guid, StorefrontPendingPaymentProjector.PaymentInput>
            {
                [checkout] = new(Guid.NewGuid(), "Pending", "manual", Now.AddMinutes(-5), 1000, "IRR"),
            },
            Cycle(checkout, 1, ReservationCycleStatus.Active, 400, expires: Now.AddMinutes(7)),
            Now);
        var item = Assert.Single(page.Items);
        Assert.True(item.IsManualAwaitingReview);
        Assert.Equal("none", item.PrimaryAction);
        Assert.False(item.CanInitiatePayment);
        Assert.False(item.CanRetryPayment);
        Assert.Equal("awaitingReview", item.PaymentPresentation);
    }

    [Fact]
    public void Multiple_pending_orders_are_independently_scoped()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var page = StorefrontPendingPaymentProjector.Project(
            [Checkout(a, "SO-A"), Checkout(b, "SO-B")],
            Merge(Pay(a, "Pending", "sandbox"), Pay(b, "Failed", "sandbox")),
            Merge(
                Cycle(a, 1, ReservationCycleStatus.Active, 100, expires: Now.AddMinutes(2)),
                Cycle(b, 1, ReservationCycleStatus.Active, 400, expires: Now.AddMinutes(8))),
            Now);
        Assert.Equal(2, page.Items.Count);
        Assert.Contains(page.Items, x => x.OrderReference == "SO-A" && x.SecondsRemaining == 100);
        Assert.Contains(page.Items, x => x.OrderReference == "SO-B" && x.SecondsRemaining == 400 && x.PaymentPresentation == "failedRetryable");
        Assert.All(page.Items, x => Assert.Equal("pay", x.PrimaryAction));
    }

    [Fact]
    public void Failed_payment_keeps_same_cycle_countdown()
    {
        var checkout = Guid.NewGuid();
        var expires = Now.AddMinutes(10);
        var before = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout, "SO-FAIL")],
            Pay(checkout, "Pending", "sandbox"),
            Cycle(checkout, 1, ReservationCycleStatus.Active, 600, expires: expires),
            Now).Items.Single();
        var after = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout, "SO-FAIL")],
            Pay(checkout, "Failed", "sandbox"),
            Cycle(checkout, 1, ReservationCycleStatus.Active, 600, expires: expires),
            Now).Items.Single();
        Assert.Equal(before.HoldEndsAt, after.HoldEndsAt);
        Assert.Equal(before.SecondsRemaining, after.SecondsRemaining);
        Assert.Equal(before.CycleNumber, after.CycleNumber);
        Assert.Equal("pay", after.PrimaryAction);
        Assert.Equal("failedRetryable", after.PaymentPresentation);
    }

    [Fact]
    public void Expired_retry_uses_same_order_and_retry_action()
    {
        var checkout = Guid.NewGuid();
        var page = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout, "SO-EXP")],
            Pay(checkout, "Expired", "sandbox"),
            Cycle(checkout, 1, ReservationCycleStatus.Expired, 0, created: 1, remaining: 2, expires: Now.AddMinutes(-1)),
            Now);
        var item = Assert.Single(page.Items);
        Assert.Equal("retryAfterExpiry", item.PrimaryAction);
        Assert.True(item.CanRetryPayment);
        Assert.False(item.CanInitiatePayment);
        Assert.Equal("ended", item.ReservationPresentation);
        Assert.Equal(checkout, item.CheckoutId);
    }

    [Fact]
    public void Max_cycles_hides_retry()
    {
        var checkout = Guid.NewGuid();
        var page = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout, "SO-MAX")],
            Pay(checkout, "Expired", "sandbox"),
            Cycle(checkout, 3, ReservationCycleStatus.Expired, 0, created: 3, max: 3, remaining: 0),
            Now);
        var item = Assert.Single(page.Items);
        Assert.Equal("none", item.PrimaryAction);
        Assert.True(item.HasReachedRetryLimit);
        Assert.False(item.CanRetryPayment);
        Assert.Equal("retryLimit", item.PaymentPresentation);
    }

    [Fact]
    public void Cancelled_and_refunded_are_hidden()
    {
        var cancelled = Guid.NewGuid();
        var refunded = Guid.NewGuid();
        var page = StorefrontPendingPaymentProjector.Project(
            [
                new StorefrontPendingPaymentProjector.CheckoutInput(
                    cancelled, Guid.NewGuid(), Guid.NewGuid(), Now,
                    [new("SO-C", SellerOrderStatus.Cancelled, 10, "IRR", [new("کالا", 1, null)])]),
                Checkout(refunded, "SO-R"),
            ],
            Pay(refunded, "Refunded", "sandbox"),
            new Dictionary<Guid, ReservationCycleProjection>(),
            Now);
        Assert.Empty(page.Items);
    }

    [Fact]
    public void Released_by_cancel_is_hidden_even_if_seller_status_lags()
    {
        var checkout = Guid.NewGuid();
        var page = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout, "SO-REL")],
            Pay(checkout, "Pending", "sandbox"),
            Cycle(checkout, 1, ReservationCycleStatus.ReleasedByCancel, 0),
            Now);
        Assert.Empty(page.Items);
    }

    [Fact]
    public void Composer_batches_projections_and_does_not_use_active_cart_secret()
    {
        var root = FindRepoRoot();
        var composer = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "Services", "StorefrontPendingPaymentService.cs"));
        var endpoints = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "Storefront", "StorefrontOrderEndpoints.cs"));
        var cart = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "storefront", "storefront-cart.tsx"));
        var pendingUi = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "storefront", "storefront-pending-payments.tsx"));
        var pendingApi = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "storefront", "storefront-pending-payment-api.ts"));
        Assert.Contains("GetProjectionsAsync", composer, StringComparison.Ordinal);
        Assert.Contains("GetLatestByCheckoutIdsAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("GetLatestForCheckoutAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProjectionAsync(", composer, StringComparison.Ordinal);
        Assert.Contains("TryGetForOwnershipAsync(group.CartId, proof.GuestSecret", composer, StringComparison.Ordinal);
        Assert.Contains("listCommittedCheckoutProofs", pendingApi, StringComparison.Ordinal);
        Assert.DoesNotContain("readCartSession()", pendingApi, StringComparison.Ordinal);
        Assert.Contains("/pending-payments", endpoints, StringComparison.Ordinal);
        Assert.Contains("/checkout/{checkoutId:guid}/cancel", endpoints, StringComparison.Ordinal);
        Assert.Contains("/checkout/{checkoutId:guid}/hide-pending-card", endpoints, StringComparison.Ordinal);
        Assert.Contains("ReadOptionalCartIdAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("HidePendingCardAsync", composer, StringComparison.Ordinal);
        Assert.Contains("PendingPaymentCardHides", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Storefront", "StorefrontOrderStores.cs")), StringComparison.Ordinal);
        Assert.Contains("pending.hide.active_hold", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "StorefrontOrderErrors.cs")), StringComparison.Ordinal);
        Assert.Contains("LoadHiddenCheckoutIdsAsync", composer, StringComparison.Ordinal);
        Assert.Contains("CancelSellerOrderAsync", composer, StringComparison.Ordinal);
        Assert.Contains("AbortForCheckoutCancelAsync", composer, StringComparison.Ordinal);
        Assert.Contains("CloseOrStartRefundForOrderCancelAsync", composer, StringComparison.Ordinal);
        Assert.Contains("ReleasedByCancel", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "Services", "StorefrontPendingPaymentProjector.cs")), StringComparison.Ordinal);
        Assert.Contains("cancelStorefrontPendingCheckout", pendingApi, StringComparison.Ordinal);
        Assert.Contains("لغو سفارش", pendingUi, StringComparison.Ordinal);
        Assert.Contains("window.confirm", pendingUi, StringComparison.Ordinal);
        Assert.Contains("StorefrontPendingPayments", cart, StringComparison.Ordinal);
        Assert.Contains("سبد فعال شما خالی است", cart, StringComparison.Ordinal);
        Assert.Contains("در انتظار پرداخت", pendingUi, StringComparison.Ordinal);
        Assert.Contains("Awaiting payment", pendingUi, StringComparison.Ordinal);
        Assert.Contains("remainingSecondsFromServer", pendingUi, StringComparison.Ordinal);
        Assert.Contains("setInterval", pendingUi, StringComparison.Ordinal);
        Assert.DoesNotContain("setInterval(() => fetch", pendingUi, StringComparison.Ordinal);
        Assert.DoesNotContain("1500", pendingUi, StringComparison.Ordinal);
        Assert.Contains("shouldRefreshOnceAtZero", pendingUi, StringComparison.Ordinal);
        Assert.DoesNotContain("ReservationCycleStatus", pendingUi, StringComparison.Ordinal);
        Assert.Contains("retryStorefrontUnpaidPayment(item.paymentId, item.checkoutId)", pendingUi, StringComparison.Ordinal);
        Assert.Contains("persistCommittedCheckoutAndDetachActiveCart", File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "storefront", "storefront-cart-api.ts")), StringComparison.Ordinal);
        Assert.Contains("CanInitiatePayment", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "Models", "StorefrontOrderModels.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain("TB-P10-T005", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Locks_and_error_copy_are_wired()
    {
        var root = FindRepoRoot();
        var locks = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-LOCKS.md"));
        var paymentCodes = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Payment", "Tooba.Payment.Application", "Errors", "PaymentErrorCodes.cs"));
        var reservation = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "ReservationCycle", "Contracts", "ReservationCycleContracts.cs"));
        var pendingApi = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "storefront", "storefront-pending-payment-api.ts"));
        var customer = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "Errors", "OrderErrorCatalogContributor.cs"));
        Assert.Contains("LOCK-SF-085", locks, StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-090", locks, StringComparison.Ordinal);
        Assert.Contains("payment.access.denied", paymentCodes, StringComparison.Ordinal);
        Assert.Contains("این سفارش در حال حاضر قابل تأمین نیست.", customer, StringComparison.Ordinal);
        Assert.Contains("تعداد دفعات مجاز رزرو مجدد موجودی برای این سفارش به پایان رسیده است.", reservation, StringComparison.Ordinal);
        Assert.Contains("پرداخت این سفارش قبلاً با موفقیت انجام شده است.", pendingApi, StringComparison.Ordinal);
    }

    private static StorefrontPendingPaymentProjector.CheckoutInput Checkout(Guid id, string orderNumber) =>
        new(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now,
            [new(orderNumber, SellerOrderStatus.PendingPayment, 250000, "IRR", [new("کالا", 1, Guid.NewGuid())])]);

    private static Dictionary<Guid, StorefrontPendingPaymentProjector.PaymentInput> Pay(
        Guid checkout,
        string status,
        string provider) =>
        new()
        {
            [checkout] = new(Guid.NewGuid(), status, provider, null, 250000, "IRR"),
        };

    private static Dictionary<Guid, ReservationCycleProjection> Cycle(
        Guid checkout,
        int number,
        ReservationCycleStatus status,
        int seconds,
        int created = 1,
        int max = 3,
        int remaining = 2,
        DateTimeOffset? expires = null) =>
        new()
        {
            [checkout] = new ReservationCycleProjection(
                checkout,
                number,
                status,
                Now.AddMinutes(-5),
                expires,
                Now,
                seconds,
                created,
                max,
                remaining,
                [],
                null),
        };

    private static Dictionary<Guid, T> Merge<T>(Dictionary<Guid, T> left, Dictionary<Guid, T> right)
    {
        var map = new Dictionary<Guid, T>(left);
        foreach (var pair in right)
        {
            map[pair.Key] = pair.Value;
        }

        return map;
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
