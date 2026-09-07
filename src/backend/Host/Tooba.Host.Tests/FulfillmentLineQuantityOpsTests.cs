using Tooba.Fulfillment.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>تست‌های خط/تعداد fulfillment برای TB-P09-T005.</summary>
public sealed class FulfillmentLineQuantityOpsTests
{
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
        Assert.Equal(FulfillmentStatus.Packed, unit.Status);
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
}
