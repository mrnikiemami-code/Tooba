using Tooba.Fulfillment.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T005: line/quantity pack, unpack, cancel release, split allocation.</summary>
public sealed class FulfillmentLineQuantityOperationsTests
{
    private static FulfillmentUnit CreateUnit(params (Guid LineId, int Qty)[] lines)
    {
        var now = DateTimeOffset.Parse("2026-09-07T06:00:00Z");
        return FulfillmentUnit.CreateFromPaidOrder(
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
            lines.Select(x => (x.LineId, x.Qty, (Guid?)Guid.NewGuid())).ToArray(),
            now);
    }

    [Fact]
    public void Whole_group_pack_packs_all_remaining()
    {
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var unit = CreateUnit((lineA, 2), (lineB, 1));
        var now = DateTimeOffset.UtcNow;
        unit.MarkPacked(now);
        Assert.Equal(FulfillmentStatus.Packed, unit.Status);
        Assert.Equal(2, unit.Items.Single(x => x.OrderLineId == lineA).QuantityPacked);
        Assert.Equal(1, unit.Items.Single(x => x.OrderLineId == lineB).QuantityPacked);
    }

    [Fact]
    public void Partial_pack_leaves_remaining_packable()
    {
        var line = Guid.NewGuid();
        var unit = CreateUnit((line, 5));
        var now = DateTimeOffset.UtcNow;
        unit.PackSelections([(line, 3)], now);
        Assert.Equal(3, unit.Items.Single().QuantityPacked);
        unit.PackSelections([(line, 2)], now);
        Assert.Equal(5, unit.Items.Single().QuantityPacked);
    }

    [Fact]
    public void Unpack_before_allocation_succeeds()
    {
        var line = Guid.NewGuid();
        var unit = CreateUnit((line, 4));
        var now = DateTimeOffset.UtcNow;
        unit.PackSelections([(line, 4)], now);
        unit.UnpackSelections([(line, 2)], now);
        Assert.Equal(2, unit.Items.Single().QuantityPacked);
    }

    [Fact]
    public void Unpack_after_allocation_rejects()
    {
        var line = Guid.NewGuid();
        var unit = CreateUnit((line, 3));
        var now = DateTimeOffset.UtcNow;
        unit.PackSelections([(line, 3)], now);
        _ = unit.CreateShipment("پست", [(line, 2)], now);
        var ex = Assert.Throws<InvalidOperationException>(() => unit.UnpackSelections([(line, 2)], now));
        Assert.Contains("تخصیص", ex.Message, StringComparison.Ordinal);
        Assert.Equal(3, unit.Items.Single().QuantityPacked);
    }

    [Fact]
    public void Shipment_from_subset_and_split_line_across_shipments()
    {
        var line = Guid.NewGuid();
        var unit = CreateUnit((line, 5));
        var now = DateTimeOffset.UtcNow;
        unit.PackSelections([(line, 5)], now);
        var s1 = unit.CreateShipment("پست", [(line, 2)], now);
        var s2 = unit.CreateShipment("تیپاکس", [(line, 3)], now);
        Assert.Equal(2, unit.Shipments.Count);
        Assert.Equal(2, s1.Items.Single().Quantity);
        Assert.Equal(3, s2.Items.Single().Quantity);
    }

    [Fact]
    public void Over_allocation_rejects()
    {
        var line = Guid.NewGuid();
        var unit = CreateUnit((line, 2));
        var now = DateTimeOffset.UtcNow;
        unit.PackSelections([(line, 2)], now);
        _ = unit.CreateShipment("A", [(line, 2)], now);
        var ex = Assert.Throws<InvalidOperationException>(() => unit.CreateShipment("B", [(line, 1)], now));
        Assert.Contains("باقیمانده", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Cancel_pre_dispatch_releases_allocation_for_recreate()
    {
        var line = Guid.NewGuid();
        var unit = CreateUnit((line, 2));
        var now = DateTimeOffset.UtcNow;
        unit.PackSelections([(line, 2)], now);
        var shipment = unit.CreateShipment("A", [(line, 2)], now);
        unit.CancelShipment(shipment.ShipmentId, now);
        Assert.Equal(ShipmentStatus.Cancelled, unit.Shipments.Single().Status);
        var again = unit.CreateShipment("B", [(line, 2)], now);
        Assert.Equal(ShipmentStatus.Created, again.Status);
    }

    [Fact]
    public void Cancel_post_dispatch_rejects()
    {
        var line = Guid.NewGuid();
        var unit = CreateUnit((line, 1));
        var now = DateTimeOffset.UtcNow;
        unit.PackSelections([(line, 1)], now);
        var shipment = unit.CreateShipment("A", [(line, 1)], now);
        unit.AssignTracking(shipment.ShipmentId, "TRK-1", now);
        unit.ApplyShipmentDispatched(shipment.ShipmentId, now);
        var ex = Assert.Throws<InvalidOperationException>(() => unit.CancelShipment(shipment.ShipmentId, now));
        Assert.Contains("پس از ارسال", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Cross_seller_line_is_rejected_by_domain()
    {
        var line = Guid.NewGuid();
        var foreign = Guid.NewGuid();
        var unit = CreateUnit((line, 1));
        var now = DateTimeOffset.UtcNow;
        var ex = Assert.Throws<InvalidOperationException>(() => unit.PackSelections([(foreign, 1)], now));
        Assert.Contains("فروشنده", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Duplicate_line_ids_are_normalized_before_pack()
    {
        var line = Guid.NewGuid();
        var unit = CreateUnit((line, 5));
        var now = DateTimeOffset.UtcNow;
        unit.PackSelections([(line, 2), (line, 1)], now);
        Assert.Equal(3, unit.Items.Single().QuantityPacked);
    }
}
