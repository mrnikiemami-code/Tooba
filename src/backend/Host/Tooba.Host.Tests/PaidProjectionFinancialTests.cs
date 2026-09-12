using Tooba.Order.Application;
using Tooba.Payment.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T004-R11 — معادلهٔ Payable و تصویر Paid شامل StoreShipping.</summary>
public sealed class PaidProjectionFinancialTests
{
    [Fact]
    public void Canonical_payable_is_seller_merchandise_plus_store_shipping()
    {
        Assert.Equal(201_088m, CheckoutPayableInvariant.CanonicalAmount([1_088m], 200_000m));
        Assert.Equal(301_000m, CheckoutPayableInvariant.CanonicalAmount([100_000m, 1_000m], 200_000m));
        Assert.Equal(207_100m, CheckoutPayableInvariant.CanonicalAmount([109_000m, 98_100m], 0m));
        Assert.Equal(12.3456m + 7.89m, CheckoutPayableInvariant.CanonicalAmount([12.3456m], 7.89m));
    }

    [Fact]
    public void Handler_uses_canonical_payable_not_seller_totals_alone()
    {
        var handler = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderPaymentSucceededHandler.cs");
        Assert.Contains("CheckoutPayableInvariant.CanonicalAmount", handler, StringComparison.Ordinal);
        Assert.Contains("ShippingAmount", handler, StringComparison.Ordinal);
        Assert.Contains("EventId", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("targets.Sum(x => x.GrandTotalSnapshot) ==", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Payment_open_keeps_store_shipping_off_seller_payout()
    {
        var sellerA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000001");
        var sellerB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-000000000002");
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            301_000m,
            "IRR",
            "fake",
            "r11-alloc",
            [
                (PaymentAllocationTargetKind.SellerOrder, sellerA, 100_000m),
                (PaymentAllocationTargetKind.SellerOrder, sellerB, 1_000m),
                (PaymentAllocationTargetKind.StoreShipping, PaymentAllocation.StoreShippingTargetId, 200_000m),
            ],
            DateTimeOffset.UtcNow);
        Assert.Equal(301_000m, payment.Amount);
        Assert.Equal(payment.Amount, payment.Allocations.Sum(x => x.AllocatedAmount));
        Assert.Equal(101_000m, payment.Allocations.Where(x => x.IsSellerOrder).Sum(x => x.AllocatedAmount));
        Assert.Equal(200_000m, payment.Allocations.Single(x => x.TargetKind == PaymentAllocationTargetKind.StoreShipping).AllocatedAmount);
        var attempt = payment.RecordInitiation("ref-r11", DateTimeOffset.UtcNow);
        payment.ClearDomainEvents();
        Assert.True(payment.ApplyVerifiedSuccess(attempt.AttemptId, "txn-r11", DateTimeOffset.UtcNow));
        var succeeded = Assert.Single(payment.DomainEvents.OfType<PaymentSucceededDomainEvent>());
        Assert.Equal(2, succeeded.SellerOrderIds.Count);
        Assert.DoesNotContain(PaymentAllocation.StoreShippingTargetId, succeeded.SellerOrderIds);
    }

    [Fact]
    public void Settlement_and_initiate_exclude_store_shipping_from_seller_ids()
    {
        var settlement = Read("src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/SettlementEventHandlers.cs");
        Assert.Contains("integrationEvent.SellerOrderIds", settlement, StringComparison.Ordinal);
        var directory = Read("src/backend/Modules/Payment/Tooba.Payment.Infrastructure/PaymentDirectory.cs");
        Assert.Contains("PaymentAllocationTargetKind.StoreShipping", directory, StringComparison.Ordinal);
        Assert.Contains("PaymentAllocation.StoreShippingTargetId", directory, StringComparison.Ordinal);
        Assert.Contains("OrderBy(x => x.SellerOrderId)", directory, StringComparison.Ordinal);
        var inbox = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderPaymentSucceededHandler.cs");
        Assert.Contains("PaymentInbox.AnyAsync", inbox, StringComparison.Ordinal);
    }

    [Fact]
    public void R10_retry_and_late_capture_surfaces_remain()
    {
        Assert.Contains("EnsureUnpaidRetryHold", Read("src/backend/Modules/Inventory/Tooba.Inventory.Application/OrderSupplyContracts.cs"), StringComparison.Ordinal);
        Assert.Contains("EnsurePaidDurableOrKeepPaidAsync", Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderPaymentBridge.cs"), StringComparison.Ordinal);
        Assert.Contains("ExpireUnpaidTimeout", Read("src/backend/Modules/Payment/Tooba.Payment.Domain/PaymentDomain.cs"), StringComparison.Ordinal);
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
