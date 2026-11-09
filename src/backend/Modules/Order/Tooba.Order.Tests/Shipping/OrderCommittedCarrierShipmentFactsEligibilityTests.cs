using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Infrastructure.Integrations.Shipping;
using Xunit;

namespace Tooba.Order.Tests.Shipping;

public sealed class OrderCommittedCarrierShipmentFactsEligibilityTests
{
    private static readonly Guid SellerOrder = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid Group = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid LineA = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid LineB = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");
    private static readonly Guid FulfillmentId = Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee");

    [Fact]
    public void Package_incomplete_still_reports_fulfillment_exhausted()
    {
        var snapshot = Snapshot(
            FulfillmentOperationStatus.Delivered,
            packedA: 2m,
            packedB: 1m,
            openAllocations: Array.Empty<(Guid, decimal, Guid?)>());

        var (reason, lines, _, packedTotal, _) = OrderCommittedCarrierShipmentFactsReader.ResolveFulfillmentEligibility(
            new[] { snapshot },
            SellerOrder,
            Group,
            new[] { LineA, LineB });

        Assert.Equal("fulfillment_exhausted", reason);
        Assert.Empty(lines);
        Assert.Equal(3m, packedTotal);
    }

    [Fact]
    public void Remaining_packed_quantities_are_exact_and_group_filtered()
    {
        var snapshot = Snapshot(
            FulfillmentOperationStatus.Packed,
            packedA: 5m,
            packedB: 3m,
            openAllocations:
            [
                (LineA, 2m, Group),
                (LineB, 3m, Guid.Parse("ffffffff-ffff-4fff-8fff-ffffffffffff")),
            ]);

        var (reason, lines, fulfillmentId, packedTotal, packedValues) =
            OrderCommittedCarrierShipmentFactsReader.ResolveFulfillmentEligibility(
                new[] { snapshot },
                SellerOrder,
                Group,
                new[] { LineA, LineB });

        Assert.Null(reason);
        Assert.Equal(FulfillmentId, fulfillmentId);
        Assert.Equal(8m, packedTotal);
        Assert.Equal(2, lines.Count);
        Assert.Equal(2, packedValues.Count);
        Assert.Contains(lines, x => x.OrderLineId == LineA && x.RemainingPackedQuantity == 3m);
        Assert.Contains(lines, x => x.OrderLineId == LineB && x.RemainingPackedQuantity == 3m);
    }

    // ---- TB-257 B: shipped quantity is really consumed -----------------------------------------

    [Fact]
    public void Wholly_shipped_group_is_exhausted_and_offers_no_executable_line()
    {
        // The reported defect: packed 1 / shipped 1 with no open allocation still advertised a line.
        var snapshot = SnapshotShipped(
            FulfillmentOperationStatus.Dispatched,
            packed: 1m,
            shipped: 1m,
            shipments:
            [
                (ShipmentOperationStatus.Dispatched, (LineA, 1m)),
            ]);

        var (reason, lines, fulfillmentId, packedTotal, packedValues) =
            OrderCommittedCarrierShipmentFactsReader.ResolveFulfillmentEligibility(
                new[] { snapshot },
                SellerOrder,
                Group,
                new[] { LineA });

        Assert.Equal("fulfillment_exhausted", reason);
        Assert.Empty(lines);
        Assert.Equal(FulfillmentId, fulfillmentId);
        Assert.Equal(1m, packedTotal);
        Assert.Single(packedValues);
    }

    [Fact]
    public void Partial_shipment_with_an_open_allocation_offers_only_the_real_remainder()
    {
        var snapshot = SnapshotShipped(
            FulfillmentOperationStatus.Dispatched,
            packed: 5m,
            shipped: 2m,
            shipments:
            [
                (ShipmentOperationStatus.Created, (LineA, 1m)),
                (ShipmentOperationStatus.Dispatched, (LineA, 2m)),
            ]);

        var (reason, lines, _, _, _) = OrderCommittedCarrierShipmentFactsReader.ResolveFulfillmentEligibility(
            new[] { snapshot },
            SellerOrder,
            Group,
            new[] { LineA });

        Assert.Null(reason);
        var line = Assert.Single(lines);
        Assert.Equal(LineA, line.OrderLineId);
        Assert.Equal(2m, line.RemainingPackedQuantity);
        Assert.Equal(5m, line.PackedQuantity);    }

    [Fact]
    public void Cancelled_pre_dispatch_allocation_releases_its_remainder()
    {
        var snapshot = SnapshotShipped(
            FulfillmentOperationStatus.Packed,
            packed: 3m,
            shipped: 0m,
            shipments:
            [
                (ShipmentOperationStatus.Cancelled, (LineA, 3m)),
            ]);

        var (reason, lines, _, _, _) = OrderCommittedCarrierShipmentFactsReader.ResolveFulfillmentEligibility(
            new[] { snapshot },
            SellerOrder,
            Group,
            new[] { LineA });

        Assert.Null(reason);
        Assert.Equal(3m, Assert.Single(lines).RemainingPackedQuantity);
    }

    private static FulfillmentSnapshot SnapshotShipped(
        FulfillmentOperationStatus status,
        decimal packed,
        decimal shipped,
        IReadOnlyList<(ShipmentOperationStatus Status, (Guid LineId, decimal Quantity) Line)> shipments)
        => new(
            FulfillmentId,
            SellerOrder,
            Guid.Parse("99999999-9999-4999-8999-999999999999"),
            Guid.Parse("88888888-8888-4888-8888-888888888888"),
            status,
            "r",
            "0",
            "p",
            "c",
            "a",
            "1",
            "m",
            "m",
            new[]
            {
                new FulfillmentItemSnapshot(Guid.NewGuid(), LineA, packed, shipped, null, packed, 0),
            },
            shipments.Select((s, index) => new ShipmentSnapshot(
                Guid.Parse($"20000000-0000-4000-8000-{index:D12}"),
                s.Status,
                "carrier",
                null,
                null,
                null,
                new[] { new ShipmentLineSnapshot(s.Line.LineId, s.Line.Quantity) },
                DateTimeOffset.UtcNow,
                "m",
                "m",
                null,
                0,
                null,
                Group)).ToArray(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private static FulfillmentSnapshot Snapshot(
        FulfillmentOperationStatus status,
        decimal packedA,
        decimal packedB,
        IReadOnlyList<(Guid LineId, decimal Qty, Guid? ShippingGroupId)> openAllocations)
    {
        var shipments = openAllocations
            .GroupBy(x => x.ShippingGroupId)
            .Select((g, index) => new ShipmentSnapshot(
                Guid.Parse($"10000000-0000-4000-8000-{index:D12}"),
                ShipmentOperationStatus.Created,
                "carrier",
                null,
                null,
                null,
                g.Select(x => new ShipmentLineSnapshot(x.LineId, x.Qty)).ToArray(),
                DateTimeOffset.UtcNow,
                "m",
                "m",
                null,
                0,
                null,
                g.Key))
            .ToArray();

        return new FulfillmentSnapshot(
            FulfillmentId,
            SellerOrder,
            Guid.Parse("99999999-9999-4999-8999-999999999999"),
            Guid.Parse("88888888-8888-4888-8888-888888888888"),
            status,
            "r",
            "0",
            "p",
            "c",
            "a",
            "1",
            "m",
            "m",
            new[]
            {
                new FulfillmentItemSnapshot(Guid.NewGuid(), LineA, packedA, 0, null, packedA, 0),
                new FulfillmentItemSnapshot(Guid.NewGuid(), LineB, packedB, 0, null, packedB, 0),
            },
            shipments,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }
}
