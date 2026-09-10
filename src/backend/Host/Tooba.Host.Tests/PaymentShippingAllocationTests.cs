using Tooba.Payment.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P10-T003-R1 — تخصیص ارسال Store نباید به اولین فروشنده بچسبد.
/// </summary>
public sealed class PaymentShippingAllocationTests
{
    [Fact]
    public void Open_with_store_shipping_keeps_seller_merchandise_separate()
    {
        var sellerA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000001");
        var sellerB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-000000000002");
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            301_000m,
            "IRR",
            "manual",
            "idem-1",
            [
                (PaymentAllocationTargetKind.SellerOrder, sellerA, 100_000m),
                (PaymentAllocationTargetKind.SellerOrder, sellerB, 1_000m),
                (PaymentAllocationTargetKind.StoreShipping, PaymentAllocation.StoreShippingTargetId, 200_000m),
            ],
            DateTimeOffset.UtcNow);

        Assert.Equal(301_000m, payment.Amount);
        Assert.Equal(101_000m, payment.Allocations.Where(x => x.IsSellerOrder).Sum(x => x.AllocatedAmount));
        Assert.Equal(200_000m, payment.Allocations.Single(x => x.TargetKind == PaymentAllocationTargetKind.StoreShipping).AllocatedAmount);
        Assert.DoesNotContain(payment.Allocations, x => x.IsSellerOrder && x.AllocatedAmount == 300_000m);
    }

    [Fact]
    public void Open_seller_order_enumeration_does_not_move_shipping()
    {
        var sellerA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000001");
        var sellerB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-000000000002");
        var rowsForward = new[]
        {
            (PaymentAllocationTargetKind.SellerOrder, sellerA, 50_000m),
            (PaymentAllocationTargetKind.SellerOrder, sellerB, 70_000m),
            (PaymentAllocationTargetKind.StoreShipping, PaymentAllocation.StoreShippingTargetId, 10_000m),
        };
        var rowsReversed = new[]
        {
            (PaymentAllocationTargetKind.SellerOrder, sellerB, 70_000m),
            (PaymentAllocationTargetKind.SellerOrder, sellerA, 50_000m),
            (PaymentAllocationTargetKind.StoreShipping, PaymentAllocation.StoreShippingTargetId, 10_000m),
        };

        var forward = CustomerPayment.Open(Guid.NewGuid(), 130_000m, "IRR", "manual", "idem-f", rowsForward, DateTimeOffset.UtcNow);
        var reversed = CustomerPayment.Open(Guid.NewGuid(), 130_000m, "IRR", "manual", "idem-r", rowsReversed, DateTimeOffset.UtcNow);

        decimal SellerAmt(CustomerPayment p, Guid id) =>
            p.Allocations.Single(x => x.IsSellerOrder && x.SellerOrderId == id).AllocatedAmount;

        Assert.Equal(SellerAmt(forward, sellerA), SellerAmt(reversed, sellerA));
        Assert.Equal(SellerAmt(forward, sellerB), SellerAmt(reversed, sellerB));
        Assert.Equal(
            forward.Allocations.Single(x => x.TargetKind == PaymentAllocationTargetKind.StoreShipping).AllocatedAmount,
            reversed.Allocations.Single(x => x.TargetKind == PaymentAllocationTargetKind.StoreShipping).AllocatedAmount);
    }

    [Fact]
    public void Succeeded_event_seller_ids_exclude_store_shipping_target()
    {
        var sellerA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000001");
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            120_000m,
            "IRR",
            "fake",
            "idem-s",
            [
                (PaymentAllocationTargetKind.SellerOrder, sellerA, 20_000m),
                (PaymentAllocationTargetKind.StoreShipping, PaymentAllocation.StoreShippingTargetId, 100_000m),
            ],
            DateTimeOffset.UtcNow);
        var attempt = payment.RecordInitiation("ref-1", DateTimeOffset.UtcNow);
        payment.ClearDomainEvents();
        Assert.True(payment.ApplyVerifiedSuccess(attempt.AttemptId, "txn-1", DateTimeOffset.UtcNow));
        var succeeded = Assert.Single(payment.DomainEvents.OfType<PaymentSucceededDomainEvent>());
        Assert.Equal(new[] { sellerA }, succeeded.SellerOrderIds);
        Assert.DoesNotContain(PaymentAllocation.StoreShippingTargetId, succeeded.SellerOrderIds);
    }
}
