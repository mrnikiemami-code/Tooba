using Tooba.Cart.Application;
using Tooba.Inventory.Application;
using Tooba.Order.Application;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T004-R10 — مهلت پرداخت‌نشده، Settings، retry و جداسازی از SupplyStatus.</summary>
public sealed class UnpaidOrderExpiryTests
{
    [Fact]
    public void Platform_store_and_method_keys_exist()
    {
        var app = Read("src/backend/Host/Tooba.Host/appsettings.json");
        Assert.Contains("\"PersistenceHours\": 168", app, StringComparison.Ordinal);
        Assert.Contains("OnlinePaymentHoldHours", app, StringComparison.Ordinal);
        Assert.Contains("ManualPaymentInitialHoldHours", app, StringComparison.Ordinal);
        Assert.Contains("ManualPaymentReviewHoldHours", app, StringComparison.Ordinal);
        Assert.Contains("ICommerceHoldPolicy", Read("src/backend/Modules/Payment/Tooba.Payment.Application/CommerceHoldPolicyContracts.cs"), StringComparison.Ordinal);
        Assert.Contains("FindMethod", Read("src/backend/Host/Tooba.Host/CommerceHoldPolicy.cs"), StringComparison.Ordinal);
        Assert.Contains("store_hold_policy_settings", Read("src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Persistence/CatalogDbContext.cs"), StringComparison.Ordinal);
        Assert.Contains("payment_method_hold_overrides", Read("src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Persistence/PaymentDbContext.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_ux_uses_existing_admin_settings_and_fa_en()
    {
        var endpoints = Read("src/backend/Host/Tooba.Host/Admin/HoldPolicySettingsEndpoints.cs");
        Assert.Contains("/v1/admin/settings/hold-policy", endpoints, StringComparison.Ordinal);
        Assert.Contains("مدت نگهداری سبد خرید", endpoints, StringComparison.Ordinal);
        Assert.Contains("مهلت پرداخت آنلاین", endpoints, StringComparison.Ordinal);
        Assert.Contains("مهلت ثبت اطلاعات پرداخت کارت‌به‌کارت", endpoints, StringComparison.Ordinal);
        Assert.Contains("مهلت بررسی پرداخت کارت‌به‌کارت", endpoints, StringComparison.Ordinal);
        Assert.Contains("does not reserve inventory", endpoints, StringComparison.Ordinal);
        var page = Read("src/frontend/app/admin/settings/page.tsx");
        Assert.Contains("admin-settings-hold-form", page, StringComparison.Ordinal);
        Assert.Contains("admin-settings-save-holds", page, StringComparison.Ordinal);
        Assert.Contains("admin-settings-cancel-holds", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Payment:Gateway", page, StringComparison.Ordinal);
        Assert.DoesNotContain("OnlinePaymentHoldHours", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_persistence_is_independent_of_hold()
    {
        Assert.Contains("ICartPersistenceHoursSource", Read("src/backend/Modules/Cart/Tooba.Cart.Application/ICartPersistenceHoursSource.cs"), StringComparison.Ordinal);
        var cart = Read("src/backend/Modules/Cart/Tooba.Cart.Infrastructure/CartDirectory.cs");
        Assert.Contains("ResolvePersistenceTtl", cart, StringComparison.Ordinal);
        Assert.DoesNotContain("ReserveAsync", cart, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpireUnpaid_skips_succeeded_review_and_cancel()
    {
        var now = DateTimeOffset.Parse("2026-09-12T00:00:00Z");
        var payment = Open("fake", now);
        var attempt = payment.RecordInitiation("req-1", now);
        payment.AssignUnpaidTimeout(now.AddHours(-1), now);
        Assert.True(payment.ExpireUnpaidTimeout(now));
        Assert.Equal(PaymentStatus.Expired, payment.Status);
        Assert.False(payment.ExpireUnpaidTimeout(now.AddMinutes(1)));

        var paid = Open("fake", now);
        paid.RecordInitiation("req-2", now);
        paid.ApplyVerifiedSuccess(paid.Attempts.Single().AttemptId, "txn", now);
        Assert.False(paid.ExpireUnpaidTimeout(now));
        Assert.Equal(PaymentStatus.Succeeded, paid.Status);

        var manual = Open("manual", now);
        manual.RecordInitiation("req-3", now);
        manual.SubmitManualEvidence("TRK-1", null, now);
        Assert.True(manual.HasActiveManualEvidence());
        Assert.False(manual.ExpireUnpaidTimeout(now));
        Assert.Equal(PaymentStatus.Pending, manual.Status);
        _ = attempt;
    }

    [Fact]
    public void Worker_is_batched_and_skips_locked_rows()
    {
        var dir = Read("src/backend/Modules/Payment/Tooba.Payment.Infrastructure/PaymentDirectory.cs");
        Assert.Contains("ExpireDueUnpaidAsync", dir, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE SKIP LOCKED", dir, StringComparison.Ordinal);
        Assert.Contains("unpaid_timeout_at", dir, StringComparison.Ordinal);
        Assert.Contains("UnpaidOrderExpiryHostedService", Read("src/backend/Host/Tooba.Host/Program.cs"), StringComparison.Ordinal);
        Assert.Contains("ReleaseReservationsAfterManualRejectAsync", Read("src/backend/Host/Tooba.Host/UnpaidOrderExpiryHostedService.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void Retry_uses_same_order_and_ensure_supply()
    {
        Assert.Equal(4, (int)OrderSupplyMode.EnsureUnpaidRetryHold);
        var composer = Read("src/backend/Host/Tooba.Host/Storefront/StorefrontPaymentComposer.cs");
        Assert.Contains("EnsureUnpaidRetryHold", composer, StringComparison.Ordinal);
        Assert.Contains("ReopenExpiredForRetryAsync", composer, StringComparison.Ordinal);
        Assert.Contains("این سفارش در حال حاضر قابل تأمین نیست.", composer, StringComparison.Ordinal);
        var customer = Read("src/backend/Host/Tooba.Host/Customer/CustomerPanelComposer.cs");
        Assert.Contains("RetryUnpaidAsync", customer, StringComparison.Ordinal);
        Assert.Contains("PaymentExpired", customer, StringComparison.Ordinal);
        var reopen = Read("src/backend/Modules/Payment/Tooba.Payment.Infrastructure/PaymentDirectory.cs");
        Assert.Contains("RecordInitiation", reopen, StringComparison.Ordinal);
        Assert.DoesNotContain("new CustomerPayment.Open", reopen, StringComparison.Ordinal);
    }

    [Fact]
    public void Late_success_from_expired_keeps_money()
    {
        var now = DateTimeOffset.Parse("2026-09-12T00:00:00Z");
        var payment = Open("fake", now);
        var attempt = payment.RecordInitiation("req-late", now);
        Assert.True(payment.ExpireUnpaidTimeout(now));
        Assert.True(payment.ApplyVerifiedSuccess(attempt.AttemptId, "txn-late", now.AddMinutes(1)));
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        var bridge = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderPaymentBridge.cs");
        Assert.Contains("Late captured money stays Paid", bridge, StringComparison.Ordinal);
        Assert.Contains("SupplyStatus remains Unavailable", bridge, StringComparison.Ordinal);
        Assert.Contains("EnsurePaidDurableOrKeepPaidAsync", bridge, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_and_customer_ux_separate_timeout_from_supply()
    {
        var admin = Read("src/frontend/app/admin/admin-api.ts");
        Assert.Contains("مهلت پرداخت پایان یافته", admin, StringComparison.Ordinal);
        Assert.Contains("AvailableForReacquire", admin, StringComparison.Ordinal);
        var customer = Read("src/frontend/app/customer-panel/customer-api.ts");
        Assert.Contains("مهلت پرداخت این سفارش به پایان رسیده است.", customer, StringComparison.Ordinal);
        Assert.Contains("retryCustomerUnpaidOrder", customer, StringComparison.Ordinal);
        var page = Read("src/frontend/app/customer-panel/orders/[checkoutId]/page.tsx");
        Assert.Contains("customer-unpaid-retry", page, StringComparison.Ordinal);
        Assert.DoesNotContain("setInterval", page, StringComparison.Ordinal);
        var result = Read("src/frontend/app/payment/result/storefront-payment-result.tsx");
        Assert.Contains("payment-unpaid-retry", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Regression_r5_r9_surfaces_remain()
    {
        Assert.Contains("PromoteReservationsForManualPaymentReviewAsync", Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderPaymentBridge.cs"), StringComparison.Ordinal);
        Assert.Contains("EnsureOrderSupplyAsync", Read("src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/InventoryDirectory.cs"), StringComparison.Ordinal);
        Assert.Contains("GetStatusesAsync", Read("src/backend/Host/Tooba.Host/Admin/OrderSupplyComposer.cs"), StringComparison.Ordinal);
        Assert.DoesNotContain("ReserveAsync", Read("src/backend/Modules/Cart/Tooba.Cart.Infrastructure/CartDirectory.cs"), StringComparison.Ordinal);
        Assert.Contains(nameof(ICheckoutReservationHoldPolicy), typeof(ICheckoutReservationHoldPolicy).Name);
        Assert.Contains("CartLifetimeOptions", typeof(CartLifetimeOptions).Name);
    }

    [Fact]
    public void Validation_rejects_out_of_range_hours()
    {
        var endpoints = Read("src/backend/Host/Tooba.Host/Admin/HoldPolicySettingsEndpoints.cs");
        Assert.Contains("ValidateHours", endpoints, StringComparison.Ordinal);
        Assert.Contains("24 * 90", endpoints, StringComparison.Ordinal);
        Assert.Contains("24 * 30", endpoints, StringComparison.Ordinal);
    }

    private static CustomerPayment Open(string provider, DateTimeOffset at) =>
        CustomerPayment.Open(
            Guid.NewGuid(),
            1000m,
            "IRR",
            provider,
            Guid.NewGuid().ToString("N"),
            new[] { (PaymentAllocationTargetKind.SellerOrder, Guid.NewGuid(), 1000m) },
            at);

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
