using Tooba.Catalog.Domain;
using Tooba.Order.Domain;
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

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T004-R23 — سقف سفارش باز، سهمیه رزرو و تنظیمات Admin.</summary>
public sealed class CheckoutAbusePolicyTests
{
    [Theory]
    [InlineData(SellerOrderStatus.PendingPayment, true)]
    [InlineData(SellerOrderStatus.Submitted, true)]
    [InlineData(SellerOrderStatus.Paid, false)]
    [InlineData(SellerOrderStatus.Cancelled, false)]
    [InlineData(SellerOrderStatus.ReservationRequested, false)]
    public void Open_unpaid_predicate_is_order_status_only(SellerOrderStatus status, bool expected)
    {
        Assert.Equal(expected, OpenUnpaidOrderPredicate.IsOpenUnpaid(status));
    }

    [Fact]
    public void Settings_defaults_and_reject_out_of_range_without_clamp()
    {
        var now = DateTimeOffset.UtcNow;
        var settings = StoreCheckoutAbuseSettings.CreateDefault(now);
        Assert.Equal(2, settings.MaxOpenUnpaidOrdersPerCustomer);
        Assert.Equal(30, settings.ReservationCommitWindowMinutes);
        Assert.Equal(3, settings.MaxCheckoutCommitsPerCustomerInWindow);
        Assert.Throws<InvalidOperationException>(() => settings.Replace(0, 30, 3, now));
        Assert.Throws<InvalidOperationException>(() => settings.Replace(21, 30, 3, now));
        Assert.Throws<InvalidOperationException>(() => settings.Replace(2, 0, 3, now));
        Assert.Throws<InvalidOperationException>(() => settings.Replace(2, 30, 0, now));
        settings.Replace(1, 15, 2, now);
        Assert.Equal(1, settings.MaxOpenUnpaidOrdersPerCustomer);
        Assert.Equal(15, settings.ReservationCommitWindowMinutes);
        Assert.Equal(2, settings.MaxCheckoutCommitsPerCustomerInWindow);
    }

    [Fact]
    public void Churn_event_is_cycle_one_and_immutable_shape()
    {
        var ev = CheckoutReservationCommit.Create(
            StoreCheckoutAbuseSettings.SingletonId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        Assert.Equal(1, ev.ReservationCycleNumber);
        Assert.Equal("order-commit", ev.Source);
        Assert.NotEqual(Guid.Empty, ev.EventId);
    }

    [Fact]
    public void Enforcement_and_admin_boundaries_are_backend_authoritative()
    {
        var root = FindRepoRoot();
        var checkout = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Checkout", "Process", "CheckoutProcessManager.cs"));
        var host = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Checkout", "Persistence", "CheckoutSubmitHost.cs"));
        var directory = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Checkout", "Persistence", "CheckoutDirectory.cs"));
        var gate = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Checkout", "Abuse", "CheckoutAbuseGate.cs"));
        var hide = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "Services", "StorefrontPendingPaymentService.cs"));
        var endpoints = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "Errors", "OrderErrorCatalogContributor.cs"));
        var admin = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Admin", "CheckoutAbuseSettingsEndpoints.cs"));
        var settings = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "admin", "settings", "page.tsx"));
        var fe = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "storefront", "storefront-checkout-api.ts"));
        var locks = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-LOCKS.md"));

        Assert.True(
            checkout.IndexOf("EnsureCanStartInitialReservationAsync", StringComparison.Ordinal)
            < checkout.IndexOf("ReserveForCheckoutAsync", StringComparison.Ordinal));
        Assert.Contains("PrepareInitialCommit", host, StringComparison.Ordinal);
        Assert.Contains("CheckoutProcessManager", directory, StringComparison.Ordinal);
        Assert.Contains("OpenUnpaidOrderPredicate.OpenUnpaidStatuses", gate, StringComparison.Ordinal);
        Assert.Contains("CheckoutReservationCommits", gate, StringComparison.Ordinal);
        Assert.Contains("CheckoutAbuseCustomerLocks", gate, StringComparison.Ordinal);
        Assert.Contains("IClock", gate, StringComparison.Ordinal);
        Assert.Contains("IStoreCheckoutAbuseSettingsReader", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("PendingPaymentCardHides", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("PaymentAttempt", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message == \"inventory.reservation.conflict\"", checkout, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException", checkout, StringComparison.Ordinal);
        Assert.Contains("pending.hide.active_hold", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "StorefrontOrderErrors.cs")), StringComparison.Ordinal);
        var orderErrors = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "StorefrontOrderErrors.cs"));
        var catalog = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "Errors", "OrderErrorCatalogContributor.cs"));
        Assert.Contains("checkout.open_unpaid_limit_reached", orderErrors, StringComparison.Ordinal);
        Assert.Contains("checkout.reservation_commit_limit_reached", orderErrors, StringComparison.Ordinal);
        Assert.Contains("شما به حداکثر تعداد سفارش‌های در انتظار پرداخت رسیده‌اید", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("Math.Clamp", admin, StringComparison.Ordinal);
        Assert.Contains("ReservationPolicyAuditEvent", admin, StringComparison.Ordinal);
        Assert.Contains("کنترل سفارش‌های پرداخت‌نشده و سوءاستفاده از رزرو", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxOpenUnpaidOrdersPerCustomer", settings, StringComparison.Ordinal);
        Assert.Contains("checkout.open_unpaid_limit_reached", fe, StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-112", locks, StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-116", locks, StringComparison.Ordinal);
        Assert.DoesNotContain("TB-P10-T005", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxReservationCommitsPerCustomerPerOfferInWindow", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void Concurrent_last_slot_uses_row_lock_not_sleep()
    {
        var root = FindRepoRoot();
        var gate = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Checkout", "Abuse", "CheckoutAbuseGate.cs"));
        Assert.Contains("AcquireCustomerLockAsync", gate, StringComparison.Ordinal);
        Assert.Contains("TouchedAt", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("Thread.Sleep", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.Delay", gate, StringComparison.Ordinal);
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
