using System.Collections.Generic;
using Tooba.Fulfillment.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>تست‌های خط/تعداد fulfillment برای TB-P09-T005.</summary>
public sealed class FulfillmentLineQuantityOpsTests
{
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

    [Fact]
    public void Partial_decimal_pack_keeps_exact_remaining()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 2.75m, now);
        unit.PackSelections([(lineId, 1.25m)], now);
        Assert.Equal(1.25m, unit.Items.Single().QuantityPacked);
        Assert.Equal(2.75m, unit.Items.Single().QuantityProcessing);
    }

    [Fact]
    public void Whole_group_pack_packs_all_remaining()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 5, now);
        unit.MarkPacked(now);
        Assert.Equal(5, unit.Items.Single().QuantityPacked);
        Assert.Equal(FulfillmentStatus.Packed, unit.Status);
    }

    [Fact]
    public void Partial_pack_leaves_remaining_packable()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 5, now);
        unit.PackSelections([(lineId, 3)], now);
        Assert.Equal(3, unit.Items.Single().QuantityPacked);
        unit.PackSelections([(lineId, 2)], now);
        Assert.Equal(5, unit.Items.Single().QuantityPacked);
    }

    [Fact]
    public void Unpack_before_allocation_succeeds()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 4, now);
        unit.PackSelections([(lineId, 4)], now);
        unit.UnpackSelections([(lineId, 2)], now);
        Assert.Equal(2, unit.Items.Single().QuantityPacked);
        Assert.Equal(FulfillmentStatus.Processing, unit.Status);
    }

    [Fact]
    public void Unpack_after_allocation_rejects()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 3, now);
        unit.PackSelections([(lineId, 3)], now);
        _ = unit.CreateShipment("پست", [(lineId, 2)], now);
        var ex = Assert.Throws<InvalidOperationException>(() => unit.UnpackSelections([(lineId, 2)], now));
        Assert.Contains("تخصیص", ex.Message, StringComparison.Ordinal);
        Assert.Equal(3, unit.Items.Single().QuantityPacked);
    }

    [Fact]
    public void Split_line_across_shipments_and_cancel_releases()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 5, now);
        unit.PackSelections([(lineId, 5)], now);
        var first = unit.CreateShipment("پست", [(lineId, 2)], now);
        var second = unit.CreateShipment("تیپاکس", [(lineId, 3)], now);
        Assert.Equal(2, unit.Shipments.Count);
        unit.CancelShipment(first.ShipmentId, now);
        Assert.Equal(ShipmentStatus.Cancelled, first.Status);
        Assert.Equal(ShipmentStatus.Created, second.Status);
        var again = unit.CreateShipment("پیک", [(lineId, 2)], now);
        Assert.Equal(3, unit.Shipments.Count);
        Assert.Equal(2, again.Items.Single().Quantity);
    }

    [Fact]
    public void Cancel_after_dispatch_rejects()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 1, now);
        unit.PackSelections([(lineId, 1)], now);
        var shipment = unit.CreateShipment("پست", [(lineId, 1)], now);
        unit.AssignTracking(shipment.ShipmentId, "TRK-1", now);
        unit.ApplyShipmentDispatched(shipment.ShipmentId, now);
        var ex = Assert.Throws<InvalidOperationException>(() => unit.CancelShipment(shipment.ShipmentId, now));
        Assert.Contains("پس از ارسال", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateShipment_requires_packed_quantity()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 2, now);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            unit.CreateShipment("پست", [(lineId, 1)], now));
        Assert.Contains("بسته‌بندی", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Cross_line_not_on_unit_rejects()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 1, now);
        var foreign = Guid.NewGuid();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            unit.PackSelections([(foreign, 1)], now));
        Assert.Contains("فروشنده", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AbortForOrderCancel_voids_created_shipments_and_is_idempotent()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 2, now);
        unit.PackSelections([(lineId, 2)], now);
        var shipment = unit.CreateShipment("پست", [(lineId, 2)], now);
        unit.AssignTracking(shipment.ShipmentId, "TRK-1", now);
        unit.AbortForOrderCancel(now);
        Assert.Equal(FulfillmentStatus.Cancelled, unit.Status);
        Assert.Equal(ShipmentStatus.Cancelled, unit.Shipments.Single().Status);
        Assert.Equal(0, unit.Items.Single().QuantityPacked);
        Assert.Equal(0, unit.Items.Single().QuantityProcessing);
        unit.AbortForOrderCancel(now.AddMinutes(1));
        Assert.Equal(FulfillmentStatus.Cancelled, unit.Status);

        unit.ReactivateAfterOrderRestore(now.AddMinutes(2));
        Assert.Equal(FulfillmentStatus.ReadyToFulfill, unit.Status);
        Assert.Equal(0, unit.Items.Single().QuantityPacked);
        Assert.Equal(ShipmentStatus.Cancelled, unit.Shipments.Single().Status);
        var replacementReservation = Guid.NewGuid();
        unit.RebindActiveReservations(new Dictionary<Guid, Guid?> { [lineId] = replacementReservation });
        Assert.Equal(replacementReservation, unit.Items.Single().ReservationId);
        unit.RebindActiveReservations(new Dictionary<Guid, Guid?> { [lineId] = replacementReservation });
        Assert.Equal(replacementReservation, unit.Items.Single().ReservationId);
        unit.MarkProcessing(now.AddMinutes(3));
        unit.PackSelections([(lineId, 2)], now.AddMinutes(3));
        var replacement = unit.CreateShipment("پست", [(lineId, 2)], now.AddMinutes(4));
        Assert.Equal(ShipmentStatus.Created, replacement.Status);
        Assert.Equal(2, unit.Shipments.Count);
    }

    [Fact]
    public void AbortForOrderCancel_rejects_after_dispatch()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 1, now);
        unit.PackSelections([(lineId, 1)], now);
        var shipment = unit.CreateShipment("پست", [(lineId, 1)], now);
        unit.AssignTracking(shipment.ShipmentId, "TRK-1", now);
        unit.ApplyShipmentDispatched(shipment.ShipmentId, now);
        var ex = Assert.Throws<InvalidOperationException>(() => unit.AbortForOrderCancel(now));
        Assert.Equal("fulfillment.cancel.already_dispatched", ex.Message);
        Assert.NotEqual(FulfillmentStatus.Cancelled, unit.Status);
    }

    [Fact]
    public void Partial_dispatch_leaves_remaining_packable_and_shippable()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = CreateUnit(lineId, 1.25m, now);
        unit.PackSelections([(lineId, 0.50m)], now);
        var first = unit.CreateShipment("پست", [(lineId, 0.50m)], now);
        unit.AssignTracking(first.ShipmentId, "TRK-A", now);
        unit.ApplyShipmentDispatched(first.ShipmentId, now);
        Assert.Equal(FulfillmentStatus.Dispatched, unit.Status);
        Assert.True(unit.HasDispatchedQuantity());

        unit.PackSelections([(lineId, 0.75m)], now);
        Assert.Equal(1.25m, unit.Items.Single().QuantityPacked);
        var second = unit.CreateShipment("پست", [(lineId, 0.75m)], now);
        Assert.Equal(2, unit.Shipments.Count);
        unit.AssignTracking(second.ShipmentId, "TRK-B", now);
        unit.ApplyShipmentDispatched(second.ShipmentId, now);
        Assert.Equal(FulfillmentStatus.Dispatched, unit.Status);
        Assert.Equal(1.25m, unit.Items.Single().QuantityShipped);
        Assert.Equal(0.50m, first.Items.Single().Quantity);
        Assert.Equal(0.75m, second.Items.Single().Quantity);
        var abort = Assert.Throws<InvalidOperationException>(() => unit.AbortForOrderCancel(now));
        Assert.Equal("fulfillment.cancel.already_dispatched", abort.Message);
    }
}
