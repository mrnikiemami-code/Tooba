using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Host.Admin;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P09-T016: لغو کل سفارش تا اولین quantity واقعی Dispatch.
/// </summary>
public sealed class WholeOrderCancelUntilDispatchTests
{
    [Theory]
    [InlineData(SellerOrderStatus.PendingPayment)]
    [InlineData(SellerOrderStatus.Submitted)]
    [InlineData(SellerOrderStatus.ReservationRequested)]
    public void Policy_allows_open_statuses(SellerOrderStatus status)
    {
        Assert.True(SellerOrderCancellationPolicy.CanCancel(status, null));
    }

    [Fact]
    public void Policy_allows_paid_packed_created_and_tracking()
    {
        Assert.True(SellerOrderCancellationPolicy.CanCancel(
            SellerOrderStatus.Paid,
            new SellerOrderCancelFulfillmentSnapshot("Packed", 0, HasDispatchedQuantity: false)));
        Assert.True(SellerOrderCancellationPolicy.CanCancel(
            SellerOrderStatus.Paid,
            new SellerOrderCancelFulfillmentSnapshot("Packed", 1, HasDispatchedQuantity: false)));
        Assert.True(SellerOrderCancellationPolicy.CanCancel(
            SellerOrderStatus.Paid,
            new SellerOrderCancelFulfillmentSnapshot("ReadyToFulfill", 1, HasDispatchedQuantity: false)));
    }

    [Fact]
    public void Policy_blocks_any_dispatched_quantity()
    {
        Assert.False(SellerOrderCancellationPolicy.CanCancel(
            SellerOrderStatus.Paid,
            new SellerOrderCancelFulfillmentSnapshot("Dispatched", 1, HasDispatchedQuantity: true)));
        Assert.False(SellerOrderCancellationPolicy.CanCancel(
            SellerOrderStatus.Paid,
            new SellerOrderCancelFulfillmentSnapshot("InTransit", 1, HasDispatchedQuantity: true)));
        Assert.False(SellerOrderCancellationPolicy.CanCancel(
            SellerOrderStatus.Paid,
            new SellerOrderCancelFulfillmentSnapshot("Delivered", 1, HasDispatchedQuantity: true)));
        Assert.False(SellerOrderCancellationPolicy.CanCancel(
            SellerOrderStatus.Paid,
            new SellerOrderCancelFulfillmentSnapshot("Packed", 1, HasDispatchedQuantity: true)));
    }

    [Fact]
    public void Composer_allows_packed_created_and_created_with_tracking()
    {
        var order = CreatePaidOrder();
        Assert.True(AdminOrderOperationsComposer.CanCancel(order, Snapshot(order, FulfillmentStatus.Packed, 1.25m, 0m, ShipmentStatus.Created, tracking: null)));
        Assert.True(AdminOrderOperationsComposer.CanCancel(order, Snapshot(order, FulfillmentStatus.Packed, 1.25m, 0m, ShipmentStatus.Created, tracking: "TRK-1")));
        Assert.True(AdminOrderOperationsComposer.CanCancel(order, fulfillment: null));
    }

    [Fact]
    public void Composer_blocks_dispatch_in_transit_delivered_and_multi_seller()
    {
        var order = CreatePaidOrder();
        var created = Snapshot(order, FulfillmentStatus.Packed, 1.25m, 0m, ShipmentStatus.Created, tracking: "TRK-1");
        var dispatched = Snapshot(order, FulfillmentStatus.Dispatched, 1.25m, 0.50m, ShipmentStatus.Dispatched, tracking: "TRK-2", dispatchedAt: DateTimeOffset.UtcNow);
        var delivered = Snapshot(order, FulfillmentStatus.Delivered, 1.25m, 0.50m, ShipmentStatus.Delivered, tracking: "TRK-3", dispatchedAt: DateTimeOffset.UtcNow, deliveredAt: DateTimeOffset.UtcNow);
        var unitOnly = Snapshot(order, FulfillmentStatus.Dispatched, 1.25m, 0m, ShipmentStatus.Created, tracking: "TRK-4");
        Assert.False(AdminOrderOperationsComposer.CanCancel(order, dispatched));
        Assert.False(AdminOrderOperationsComposer.CanCancel(order, delivered));
        Assert.False(AdminOrderOperationsComposer.CanCancel(order, unitOnly));
        Assert.False(AdminOrderOperationsComposer.HasDispatchedOrDelivered([created]));
        Assert.True(AdminOrderOperationsComposer.HasDispatchedOrDelivered([created, dispatched]));
    }

    [Fact]
    public void Gate_source_aligns_unit_status_with_composer()
    {
        var order = CreatePaidOrder();
        var unitDispatched = Snapshot(order, FulfillmentStatus.Dispatched, 1.25m, 0m, ShipmentStatus.Created, tracking: null);
        Assert.True(AdminOrderOperationsComposer.HasDispatchedQuantity(unitDispatched));
        var packed = Snapshot(order, FulfillmentStatus.Packed, 1.25m, 0m, ShipmentStatus.Created, tracking: "TRK-1");
        Assert.False(AdminOrderOperationsComposer.HasDispatchedQuantity(packed));
        var gate = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Fulfillment",
            "Tooba.Fulfillment.Infrastructure",
            "FulfillmentSellerOrderCancelGate.cs"));
        Assert.Contains("FulfillmentStatus.Dispatched", gate, StringComparison.Ordinal);
        Assert.Contains("FulfillmentStatus.InTransit", gate, StringComparison.Ordinal);
        Assert.Contains("FulfillmentStatus.Delivered", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void Abort_voids_pre_dispatch_shipment_keeps_history_and_rejects_dispatch()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 1.25m, now);
        unit.MarkProcessing(now);
        unit.PackSelections([(lineId, 0.50m)], now);
        var shipment = unit.CreateShipment("پست", [(lineId, 0.50m)], now);
        unit.AssignTracking(shipment.ShipmentId, "TRK-T016", now);
        Assert.False(unit.HasDispatchedQuantity());
        unit.AbortForOrderCancel(now.AddMinutes(1));
        Assert.Equal(FulfillmentStatus.Cancelled, unit.Status);
        Assert.Equal(ShipmentStatus.Cancelled, unit.Shipments.Single().Status);
        Assert.Equal("TRK-T016", unit.Shipments.Single().TrackingReference);
        Assert.Equal(0.50m, unit.Items.Single().QuantityPacked);
        Assert.Equal(1.25m, unit.Items.Single().QuantityProcessing);
        Assert.Equal(0m, unit.Items.Single().QuantityShipped);

        unit.ReactivateAfterOrderRestore(now.AddMinutes(2));
        Assert.NotEqual(ShipmentStatus.Created, unit.Shipments.Single().Status);
        Assert.Equal(ShipmentStatus.Cancelled, unit.Shipments.Single().Status);

        var dispatched = CreateUnit(lineId, 1.25m, now);
        dispatched.MarkProcessing(now);
        dispatched.PackSelections([(lineId, 0.50m)], now);
        var shipped = dispatched.CreateShipment("پست", [(lineId, 0.50m)], now);
        dispatched.AssignTracking(shipped.ShipmentId, "TRK-D", now);
        dispatched.ApplyShipmentDispatched(shipped.ShipmentId, now);
        var ex = Assert.Throws<InvalidOperationException>(() => dispatched.AbortForOrderCancel(now));
        Assert.Equal("fulfillment.cancel.already_dispatched", ex.Message);
    }

    [Fact]
    public void Composer_source_keeps_direct_guard_and_human_confirm()
    {
        var root = FindRepoRoot();
        var composer = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderOperationsComposer.cs"));
        var completeness = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderCompletenessComposer.cs"));
        var checkout = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "CheckoutDirectory.cs"));
        Assert.Contains(AdminOrderOperationsComposer.WholeOrderCancelBlockedAfterDispatchFa, composer, StringComparison.Ordinal);
        Assert.Contains(AdminOrderOperationsComposer.WholeOrderCancelConfirmFa, composer, StringComparison.Ordinal);
        Assert.Contains("HasDispatchedOrDelivered(fulfillments)", composer, StringComparison.Ordinal);
        Assert.Contains("AbortForCheckoutCancelAsync", composer, StringComparison.Ordinal);
        Assert.Contains("NeutralizeUnpaidAccrualForCancelAsync", composer, StringComparison.Ordinal);
        Assert.Contains("CloseOrStartRefundForOrderCancelAsync", composer, StringComparison.Ordinal);
        Assert.Contains("ReleaseAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("سفارش لغو شد", completeness, StringComparison.Ordinal);
        Assert.Contains("مرسوله پیش از ارسال ابطال شد", completeness, StringComparison.Ordinal);
        Assert.Contains("تخصیص اقلام آزاد شد", completeness, StringComparison.Ordinal);
        Assert.Contains("رزرو موجودی آزاد شد", completeness, StringComparison.Ordinal);
        Assert.Contains("بازگشت وجه آغاز شد", completeness, StringComparison.Ordinal);
        Assert.Contains("تعدیل سهم فروشنده ثبت شد", completeness, StringComparison.Ordinal);
        Assert.DoesNotContain("آیا از لغو این سفارش مطمئن هستید؟", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("لغو پس از ارسال واقعی مرسوله مجاز نیست.", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Cancelled_blocks_pack_ship_dispatch_deliver()
    {
        Assert.Contains("mark_processing", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("mark_packed", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("pack_selected", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("create_shipment", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("dispatch_shipment", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("deliver_shipment", AdminOrderOperationsComposer.CancelledBlockedCodes);
    }

    [Fact]
    public void Human_block_and_confirm_are_backend_authoritative()
    {
        Assert.Equal(
            "پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست.",
            AdminOrderOperationsComposer.WholeOrderCancelBlockedAfterDispatchFa);
        Assert.Equal(
            "پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست.",
            AdminOrderOperationsComposer.FulfillmentOpToFa("order.cancel.forbidden"));
        Assert.Contains("مرسوله‌های پیش از ارسال ابطال می‌شوند", AdminOrderOperationsComposer.WholeOrderCancelConfirmFa, StringComparison.Ordinal);
        Assert.Contains("موجودی آزاد می‌شود", AdminOrderOperationsComposer.WholeOrderCancelConfirmFa, StringComparison.Ordinal);
        Assert.Contains("بازگشت وجه آغاز می‌شود", AdminOrderOperationsComposer.WholeOrderCancelConfirmFa, StringComparison.Ordinal);
    }

    private static SellerOrder CreatePaidOrder()
    {
        var sellerOrderId = Guid.NewGuid();
        var line = OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1.25m,
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
            Guid.NewGuid(),
            Guid.NewGuid(),
            $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase,
            "IRR",
            [line]);
        order.RecordVerifiedPayment();
        return order;
    }

    private static FulfillmentSnapshot Snapshot(
        SellerOrder order,
        FulfillmentStatus status,
        decimal ordered,
        decimal shipped,
        ShipmentStatus shipmentStatus,
        string? tracking,
        DateTimeOffset? dispatchedAt = null,
        DateTimeOffset? deliveredAt = null) =>
        new(
            Guid.NewGuid(),
            order.SellerOrderId,
            order.CheckoutId,
            order.SellerPartyId,
            status,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            [new FulfillmentItemSnapshot(Guid.NewGuid(), order.Lines[0].LineId, ordered, shipped, Guid.NewGuid(), shipped > 0 ? shipped : 0.50m, ordered)],
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    shipmentStatus,
                    "Post",
                    tracking,
                    dispatchedAt,
                    deliveredAt,
                    [new ShipmentLineSnapshot(order.Lines[0].LineId, shipped > 0 ? shipped : 0.50m)]),
            ]);

    private static FulfillmentUnit CreateUnit(Guid lineId, decimal qty, DateTimeOffset now) =>
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
            [(lineId, qty, Guid.NewGuid())],
            now);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
