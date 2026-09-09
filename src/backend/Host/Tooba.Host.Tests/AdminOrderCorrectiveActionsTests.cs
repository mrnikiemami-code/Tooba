using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Host.Admin;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Tooba.Payment.Domain;
using Tooba.Returns.Domain;
using Tooba.Settlement.Application;
using Tooba.Settlement.Domain;
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
    public void Unconfirm_deposit_after_success_returns_pending_without_new_success()
    {
        var now = DateTimeOffset.UtcNow;
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            800m,
            "IRR",
            "manual",
            $"idem-{Guid.NewGuid():N}",
            [(Guid.NewGuid(), 800m)],
            now);
        var first = payment.RecordInitiation("manual-ok", now);
        payment.ApplyVerifiedSuccess(first.AttemptId, "manual-confirm", now.AddMinutes(1));
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        var successEvents = payment.DomainEvents.Count(e => e is PaymentSucceededDomainEvent);
        var unconfirmed = payment.UnconfirmManualDeposit(now.AddMinutes(2));
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Null(payment.CompletedAt);
        Assert.Equal(PaymentAttemptStatus.Initiated, unconfirmed.Status);
        Assert.Equal(successEvents, payment.DomainEvents.Count(e => e is PaymentSucceededDomainEvent));
        Assert.Contains(payment.DomainEvents, e => e is PaymentManualDepositUnconfirmedDomainEvent);
        var again = payment.UnconfirmManualDeposit(now.AddMinutes(3));
        Assert.Equal(unconfirmed.AttemptId, again.AttemptId);
    }

    [Fact]
    public void Revert_verified_payment_returns_pending_payment()
    {
        var order = CreateSellerOrder(paid: true);
        Assert.Equal(SellerOrderStatus.Paid, order.Status);
        order.RevertVerifiedPayment();
        Assert.Equal(SellerOrderStatus.PendingPayment, order.Status);
        order.RevertVerifiedPayment();
        Assert.Equal(SellerOrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public void Unconfirm_hidden_after_fulfillment_starts()
    {
        var ready = new FulfillmentSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            FulfillmentStatus.ReadyToFulfill,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1, 0, null, 0)],
            []);
        Assert.False(AdminOrderOperationsComposer.HasStartedFulfillment([ready]));
        Assert.False(AdminOrderOperationsComposer.HasStartedFulfillment([]));

        var processing = ready with { Status = FulfillmentStatus.Processing };
        Assert.True(AdminOrderOperationsComposer.HasStartedFulfillment([processing]));

        var cancelledOnly = ready with
        {
            Shipments =
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    ShipmentStatus.Cancelled,
                    "Post",
                    "TRK",
                    null,
                    null,
                    []),
            ],
        };
        Assert.False(AdminOrderOperationsComposer.HasStartedFulfillment([cancelledOnly]));

        var packedAfterRestore = ready with
        {
            Status = FulfillmentStatus.Packed,
            Items = [ready.Items[0] with { QuantityPacked = 1, QuantityProcessing = 1 }],
            Shipments = cancelledOnly.Shipments,
        };
        Assert.True(AdminOrderOperationsComposer.HasStartedFulfillment([packedAfterRestore]));
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
        Assert.True(AdminOrderOperationsComposer.CanRestoreCancelledOrder(group, [], [], blockedBySellerPayout: false));
    }

    [Fact]
    public void Restore_forbidden_when_seller_payout_completed()
    {
        var group = SeedCancelledCheckout();
        Assert.False(AdminOrderOperationsComposer.CanRestoreCancelledOrder(group, [], [], blockedBySellerPayout: true));
        Assert.Equal(
            "order.restore.seller_payout_completed",
            AdminOrderOperationsComposer.RestoreForbiddenCode(group, [], [], blockedBySellerPayout: true));
        Assert.Equal(
            "این سفارش به‌دلیل انجام تسویه/واریز سهم فروشنده قابل بازگردانی نیست.",
            AdminOrderOperationsComposer.RestoreCodeToFa("order.restore.seller_payout_completed"));
    }

    [Fact]
    public void Restore_forbidden_when_payment_refunded_but_allowed_while_refund_pending()
    {
        var group = SeedCancelledCheckout();
        Assert.False(AdminOrderOperationsComposer.CanRestoreCancelledOrder(
            group,
            [],
            [],
            paymentStatus: PaymentStatus.Refunded));
        Assert.Equal(
            "order.restore.refund_completed",
            AdminOrderOperationsComposer.RestoreForbiddenCode(
                group,
                [],
                [],
                paymentStatus: PaymentStatus.Refunded));
        Assert.True(AdminOrderOperationsComposer.CanRestoreCancelledOrder(
            group,
            [],
            [],
            paymentStatus: PaymentStatus.RefundPending));
        Assert.True(AdminOrderOperationsComposer.CanRestoreCancelledOrder(
            group,
            [],
            [],
            paymentStatus: PaymentStatus.RefundFailed));
    }

    [Fact]
    public void Restore_after_cancel_refund_pending_returns_succeeded_without_success_event()
    {
        var now = DateTimeOffset.UtcNow;
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            1000m,
            "IRR",
            "manual",
            $"idem-{Guid.NewGuid():N}",
            [(Guid.NewGuid(), 1000m)],
            now);
        var attempt = payment.RecordInitiation("req-1", now);
        payment.ApplyVerifiedSuccess(attempt.AttemptId, "txn-1", now.AddMinutes(1));
        var successEvents = payment.DomainEvents.Count(e => e is PaymentSucceededDomainEvent);
        payment.BeginOrderCancelRefund(now.AddMinutes(2));
        Assert.Equal(PaymentStatus.RefundPending, payment.Status);
        payment.RestoreAfterOrderCancelRestore(now.AddMinutes(3));
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(successEvents, payment.DomainEvents.Count(e => e is PaymentSucceededDomainEvent));
        payment.RestoreAfterOrderCancelRestore(now.AddMinutes(4));
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(successEvents, payment.DomainEvents.Count(e => e is PaymentSucceededDomainEvent));
    }

    [Fact]
    public void Restore_after_cancel_from_pending_returns_pending()
    {
        var now = DateTimeOffset.UtcNow;
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            1000m,
            "IRR",
            "manual",
            $"idem-{Guid.NewGuid():N}",
            [(Guid.NewGuid(), 1000m)],
            now);
        payment.RecordInitiation("req-1", now);
        payment.CloseForOrderCancel(now.AddMinutes(1));
        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        payment.RestoreAfterOrderCancelRestore(now.AddMinutes(2));
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.DoesNotContain(payment.DomainEvents, e => e is PaymentSucceededDomainEvent);
    }

    [Fact]
    public void Restore_after_cancel_from_rejected_deposit_returns_failed()
    {
        var now = DateTimeOffset.UtcNow;
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            1000m,
            "IRR",
            "manual",
            $"idem-{Guid.NewGuid():N}",
            [(Guid.NewGuid(), 1000m)],
            now);
        var first = payment.RecordInitiation("manual-1", now);
        payment.ApplyVerifiedFailure(first.AttemptId, "MANUAL_DEPOSIT_REJECTED", now.AddMinutes(1));
        payment.CloseForOrderCancel(now.AddMinutes(2));
        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        payment.RestoreAfterOrderCancelRestore(now.AddMinutes(3));
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public void Restore_after_completed_refund_is_forbidden()
    {
        var now = DateTimeOffset.UtcNow;
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            1000m,
            "IRR",
            "manual",
            $"idem-{Guid.NewGuid():N}",
            [(Guid.NewGuid(), 1000m)],
            now);
        var attempt = payment.RecordInitiation("req-1", now);
        payment.ApplyVerifiedSuccess(attempt.AttemptId, "txn-1", now.AddMinutes(1));
        payment.BeginOrderCancelRefund(now.AddMinutes(2));
        payment.MarkRefunded(now.AddMinutes(3));
        var ex = Assert.Throws<InvalidOperationException>(() => payment.RestoreAfterOrderCancelRestore(now.AddMinutes(4)));
        Assert.Equal("payment.restore.refund_completed", ex.Message);
    }

    [Fact]
    public void Order_cancel_refund_does_not_un_cancel_payment_until_gateway_succeeds()
    {
        var now = DateTimeOffset.UtcNow;
        var payment = CustomerPayment.Open(
            Guid.NewGuid(),
            1000m,
            "IRR",
            "fake",
            $"idem-{Guid.NewGuid():N}",
            [(Guid.NewGuid(), 1000m)],
            now);
        var attempt = payment.RecordInitiation("req-1", now);
        payment.ApplyVerifiedSuccess(attempt.AttemptId, "txn-1", now.AddMinutes(1));
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        payment.BeginOrderCancelRefund(now.AddMinutes(2));
        Assert.Equal(PaymentStatus.RefundPending, payment.Status);
        payment.BeginOrderCancelRefund(now.AddMinutes(3));
        Assert.Equal(PaymentStatus.RefundPending, payment.Status);
        payment.MarkRefundFailed("GATEWAY_REFUND_REJECTED", now.AddMinutes(4));
        Assert.Equal(PaymentStatus.RefundFailed, payment.Status);
    }

    [Fact]
    public void Restore_refund_and_dispatch_blockers_outrank_payout()
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
        Assert.Equal(
            "order.restore.dispatched",
            AdminOrderOperationsComposer.RestoreForbiddenCode(group, [dispatched], [], blockedBySellerPayout: true));

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
        Assert.Equal(
            "order.restore.refund_completed",
            AdminOrderOperationsComposer.RestoreForbiddenCode(group, [], [refunded], blockedBySellerPayout: true));
    }

    [Fact]
    public void Accrued_unpaid_seller_amount_does_not_block_restore()
    {
        var orderId = Guid.NewGuid();
        var slices = new RestoreSettlementLedgerSlice[]
        {
            new(orderId, EntryType.Credit, 900m, DateTimeOffset.UtcNow, Guid.NewGuid()),
        };
        Assert.False(SellerOrderRestoreSettlementPolicy.HasCompletedPayoutEffect(slices, 0m, orderId));
    }

    [Fact]
    public void Completed_payout_fifo_blocks_consumed_order_only()
    {
        var older = Guid.NewGuid();
        var newer = Guid.NewGuid();
        var t0 = DateTimeOffset.Parse("2026-09-01T00:00:00Z");
        var slices = new RestoreSettlementLedgerSlice[]
        {
            new(older, EntryType.Credit, 100m, t0, Guid.NewGuid()),
            new(newer, EntryType.Credit, 50m, t0.AddHours(1), Guid.NewGuid()),
        };
        Assert.True(SellerOrderRestoreSettlementPolicy.HasCompletedPayoutEffect(slices, 80m, older));
        Assert.False(SellerOrderRestoreSettlementPolicy.HasCompletedPayoutEffect(slices, 80m, newer));
        Assert.True(SellerOrderRestoreSettlementPolicy.HasCompletedPayoutEffect(slices, 150m, newer));
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
        Assert.Contains("unconfirm_deposit", composer, StringComparison.Ordinal);
        Assert.Contains("ApplyVerifiedSuccessAsync", composer, StringComparison.Ordinal);
        Assert.Contains("EnsureCreatedForPaidCheckoutAsync", composer, StringComparison.Ordinal);
        Assert.Contains("برگشت از واریز", composer, StringComparison.Ordinal);
        Assert.Contains("برگشت از رد واریز", composer, StringComparison.Ordinal);
        Assert.Contains("restore_cancelled_order", composer, StringComparison.Ordinal);
        Assert.Contains("GetRestoreSettlementGatesAsync", composer, StringComparison.Ordinal);
        Assert.Contains("order.restore.seller_payout_completed", composer, StringComparison.Ordinal);
        Assert.Contains("if (code == \"restore_cancelled_order\")", composer, StringComparison.Ordinal);
        Assert.Contains("correct_tracking", composer, StringComparison.Ordinal);
        Assert.Contains("ProjectWholeOrderCancel", composer, StringComparison.Ordinal);
        Assert.Contains("AbortForCheckoutCancelAsync", composer, StringComparison.Ordinal);
        Assert.Contains("NeutralizeUnpaidAccrualForCancelAsync", composer, StringComparison.Ordinal);
        Assert.Contains("CloseOrStartRefundForOrderCancelAsync", composer, StringComparison.Ordinal);
        Assert.Contains("ReactivateAfterOrderRestoreAsync", composer, StringComparison.Ordinal);
        Assert.Contains("RestoreCancelledCheckoutAsync", composer, StringComparison.Ordinal);
        var restoreIdx = composer.IndexOf("RestoreCancelledCheckoutAsync", StringComparison.Ordinal);
        var reactivateIdx = composer.IndexOf("ReactivateAfterOrderRestoreAsync", StringComparison.Ordinal);
        Assert.True(restoreIdx > 0 && reactivateIdx > restoreIdx,
            "Restore must reacquire Inventory before Fulfillment reactivation/rebind.");
        Assert.Contains("RestoreAfterOrderCancelRestoreAsync", composer, StringComparison.Ordinal);
        Assert.Contains("ReinstateAccrualAfterCancelRestoreAsync", composer, StringComparison.Ordinal);
        Assert.Contains("VoidUnpaidAccrualForPaymentAsync", composer, StringComparison.Ordinal);
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

    private static FulfillmentUnit CreateUnit(Guid orderLineId, decimal quantity, DateTimeOffset now)
    {
        var unit = FulfillmentUnit.CreateFromPaidOrder(
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
        unit.MarkProcessing(now);
        return unit;
    }

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
