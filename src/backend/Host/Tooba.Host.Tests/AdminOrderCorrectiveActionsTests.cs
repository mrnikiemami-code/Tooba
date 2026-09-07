using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Host.Admin;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Tooba.Payment.Domain;
using Tooba.Returns.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T009: corrective actions, cancel snapshot, whole-order dedupe.</summary>
public sealed class AdminOrderCorrectiveActionsTests
{
    [Fact]
    public void Whole_order_collapse_keeps_one_cancel()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var actions = new[]
        {
            Act("cancel", a),
            Act("cancel", b),
            Act("cancel", null),
            Act("confirm_deposit", null),
            Act("cancel_shipment", a),
        };
        var collapsed = AdminOrderWholeOrderActions.Collapse(actions);
        Assert.Equal(3, collapsed.Count);
        Assert.Equal(1, collapsed.Count(x => x.Code == "cancel"));
        Assert.Null(collapsed.Single(x => x.Code == "cancel").SellerOrderId);
        Assert.Contains(collapsed, x => x.Code == "cancel_shipment" && x.SellerOrderId == a);
    }

    [Fact]
    public void Cancel_snapshot_then_restore_returns_prior_status()
    {
        var order = CreateSellerOrder(paid: false);
        Assert.Equal(SellerOrderStatus.PendingPayment, order.Status);
        order.Cancel();
        Assert.Equal(SellerOrderStatus.Cancelled, order.Status);
        Assert.Equal(SellerOrderStatus.PendingPayment, order.CancelledFromStatus);
        order.RestoreFromCancellation(DateTimeOffset.UtcNow);
        Assert.Equal(SellerOrderStatus.PendingPayment, order.Status);
        Assert.Null(order.CancelledFromStatus);
        Assert.NotNull(order.LastRestoredAt);
    }

    [Fact]
    public void Paid_cancel_snapshot_restores_to_paid()
    {
        var order = CreateSellerOrder(paid: true);
        order.CancelPaidBeforeShipment();
        Assert.Equal(SellerOrderStatus.Cancelled, order.Status);
        Assert.Equal(SellerOrderStatus.Paid, order.CancelledFromStatus);
        order.RestoreFromCancellation(DateTimeOffset.UtcNow);
        Assert.Equal(SellerOrderStatus.Paid, order.Status);
    }

    [Fact]
    public void Manual_reject_then_restore_returns_pending_without_success()
    {
        var now = DateTimeOffset.UtcNow;
        var sellerOrderId = Guid.NewGuid();
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            1000m,
            "IRR",
            "manual",
            $"idem-{Guid.NewGuid():N}",
            [(sellerOrderId, 1000m)],
            now);
        var first = payment.RecordInitiation("manual-1", now);
        payment.ApplyVerifiedFailure(first.AttemptId, "MANUAL_DEPOSIT_REJECTED", now.AddMinutes(1));
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        var restored = payment.RestoreRejectedManualToPending(now.AddMinutes(2));
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(PaymentAttemptStatus.Initiated, restored.Status);
        Assert.DoesNotContain(payment.DomainEvents, e => e is PaymentSucceededDomainEvent);
        Assert.Contains(payment.DomainEvents, e => e is PaymentManualDepositRestoredDomainEvent);
        var again = payment.RestoreRejectedManualToPending(now.AddMinutes(3));
        Assert.Equal(restored.AttemptId, again.AttemptId);
    }

    [Fact]
    public void Restore_deposit_forbidden_after_success()
    {
        var now = DateTimeOffset.UtcNow;
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            500m,
            "IRR",
            "manual",
            $"idem-{Guid.NewGuid():N}",
            [(Guid.NewGuid(), 500m)],
            now);
        var first = payment.RecordInitiation("manual-ok", now);
        payment.ApplyVerifiedSuccess(first.AttemptId, "manual-confirm", now.AddMinutes(1));
        var ex = Assert.Throws<InvalidOperationException>(() => payment.RestoreRejectedManualToPending(now.AddMinutes(2)));
        Assert.Equal("payment.restore.already_succeeded", ex.Message);
    }

    [Fact]
    public void Shipment_with_tracking_can_cancel_pre_dispatch_and_release_allocation()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 2, now);
        unit.PackSelections([(lineId, 2)], now);
        var shipment = unit.CreateShipment("پست", [(lineId, 2)], now);
        unit.AssignTracking(shipment.ShipmentId, "TRK-OLD", now);
        unit.CancelShipment(shipment.ShipmentId, now.AddMinutes(1));
        Assert.Equal(ShipmentStatus.Cancelled, unit.Shipments.Single().Status);
        Assert.Equal("TRK-OLD", unit.Shipments.Single().TrackingReference);
        Assert.Equal(0, unit.OpenAllocatedQuantity(lineId));
        var replacement = unit.CreateShipment("تیپاکس", [(lineId, 2)], now.AddMinutes(2));
        Assert.Equal(ShipmentStatus.Created, replacement.Status);
        Assert.Equal(2, unit.OpenAllocatedQuantity(lineId));
    }

    [Fact]
    public void Tracking_can_be_corrected_before_dispatch_and_not_after()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 1, now);
        unit.PackSelections([(lineId, 1)], now);
        var shipment = unit.CreateShipment("پست", [(lineId, 1)], now);
        unit.AssignTracking(shipment.ShipmentId, "TRK-OLD", now);
        unit.CorrectTracking(shipment.ShipmentId, "TRK-NEW", now.AddMinutes(1));
        Assert.Equal("TRK-NEW", unit.Shipments.Single().TrackingReference);
        Assert.Equal("TRK-OLD", unit.Shipments.Single().PreviousTrackingReference);
        unit.ApplyShipmentDispatched(shipment.ShipmentId, now.AddMinutes(2));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            unit.CorrectTracking(shipment.ShipmentId, "TRK-LATER", now.AddMinutes(3)));
        Assert.Equal("fulfillment.tracking.locked_after_dispatch", ex.Message);
        Assert.Equal("TRK-NEW", unit.Shipments.Single().TrackingReference);
    }

    [Fact]
    public void Restore_forbidden_after_dispatch_or_completed_refund()
    {
        var group = SeedCancelledCheckout();
        var dispatched = new FulfillmentSnapshot(
            Guid.NewGuid(),
            group.SellerOrders[0].SellerOrderId,
            group.CheckoutId,
            group.SellerOrders[0].SellerPartyId,
            FulfillmentStatus.Dispatched,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            [],
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    ShipmentStatus.Dispatched,
                    "Post",
                    "TRK",
                    DateTimeOffset.UtcNow,
                    null,
                    []),
            ]);
        Assert.False(AdminOrderOperationsComposer.CanRestoreCancelledOrder(group, [dispatched], []));
        Assert.Equal(
            "order.restore.dispatched",
            AdminOrderOperationsComposer.RestoreForbiddenCode(group, [dispatched], []));

        var refunded = ReturnRequest.Create(
            group.SellerOrders[0].SellerOrderId,
            group.CheckoutId,
            group.SellerOrders[0].SellerPartyId,
            Guid.NewGuid(),
            $"ret-{Guid.NewGuid():N}",
            "test",
            "IRR",
            [(group.SellerOrders[0].Lines[0].LineId, 1, 1000m, null)],
            DateTimeOffset.UtcNow);
        refunded.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        refunded.MarkRefundProcessing(DateTimeOffset.UtcNow);
        refunded.MarkRefundSucceeded(DateTimeOffset.UtcNow);
        Assert.False(AdminOrderOperationsComposer.CanRestoreCancelledOrder(group, [], [refunded]));
        Assert.Equal(
            "order.restore.refund_completed",
            AdminOrderOperationsComposer.RestoreForbiddenCode(group, [], [refunded]));
    }

    [Fact]
    public void Restore_allowed_when_all_sellers_cancelled_without_irreversible_effect()
    {
        var group = SeedCancelledCheckout();
        Assert.True(AdminOrderOperationsComposer.CanRestoreCancelledOrder(group, [], []));
    }

    [Fact]
    public void Unpack_before_allocation_still_allowed()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 4, now);
        unit.PackSelections([(lineId, 4)], now);
        unit.UnpackSelections([(lineId, 2)], now);
        Assert.Equal(2, unit.Items.Single().QuantityPacked);
    }

    [Fact]
    public void Composer_source_projects_restore_and_correct_tracking()
    {
        var root = FindRepoRoot();
        var composer = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderOperationsComposer.cs"));
        Assert.Contains("restore_deposit", composer, StringComparison.Ordinal);
        Assert.Contains("restore_cancelled_order", composer, StringComparison.Ordinal);
        Assert.Contains("correct_tracking", composer, StringComparison.Ordinal);
        Assert.Contains("ProjectWholeOrderCancel", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("sellerOrderId ?? throw new PlatformHttpException(400, \"شناسه سفارش فروشنده الزامی است.\"", composer, StringComparison.Ordinal);
    }

    private static AdminOrderOperationAction Act(string code, Guid? sellerOrderId) =>
        new(code, code, code, sellerOrderId, null, null, null, "order.cancel", true, null);

    private static SellerOrder CreateSellerOrder(bool paid)
    {
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var line = OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            1000m,
            "IRR",
            true,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Taxable",
            0.09m,
            90m,
            1090m,
            null);
        var order = SellerOrder.Open(
            checkoutId,
            Guid.NewGuid(),
            $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase,
            "IRR",
            [line]);
        if (paid)
        {
            order.RecordVerifiedPayment();
        }

        return order;
    }

    private static CheckoutGroup SeedCancelledCheckout()
    {
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var line = OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            1000m,
            "IRR",
            true,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Taxable",
            0.09m,
            90m,
            1090m,
            null);
        var order = SellerOrder.Open(
            checkoutId,
            Guid.NewGuid(),
            $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase,
            "IRR",
            [line]);
        order.Cancel();
        return CheckoutGroup.Submit(
            checkoutId,
            $"idem-{checkoutId:N}",
            Guid.NewGuid(),
            OrderMode.OnlinePurchase,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "IR",
            "IRR",
            SalesChannel.Marketplace,
            [order],
            DateTimeOffset.UtcNow,
            "گیرنده تست",
            "09120000000",
            "تهران",
            "تهران",
            "آدرس تست",
            "1234567890",
            "post",
            "پست");
    }

    private static FulfillmentUnit CreateUnit(Guid orderLineId, int quantity, DateTimeOffset now) =>
        FulfillmentUnit.CreateFromPaidOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "گیرنده",
            "+98912",
            "تهران",
            "تهران",
            "آدرس",
            "12345",
            "storefront-default",
            "ارسال",
            [(orderLineId, quantity, Guid.NewGuid())],
            now);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
