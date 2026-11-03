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
